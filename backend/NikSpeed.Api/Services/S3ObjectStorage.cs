using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace NikSpeed.Api.Services;

public sealed class S3ObjectStorage(HttpClient http, IConfiguration configuration) : IObjectStorage
{
    private readonly Uri endpoint = new(Require(configuration, "Storage:S3:Endpoint").TrimEnd('/') + "/");
    private readonly string bucket = Require(configuration, "Storage:S3:Bucket");
    private readonly string region = configuration["Storage:S3:Region"] ?? "auto";
    private readonly string accessKey = Require(configuration, "Storage:S3:AccessKeyId");
    private readonly string secretKey = Require(configuration, "Storage:S3:SecretAccessKey");

    public async Task<StoredMedia> SaveAsync(IFormFile file, string key, string contentType, CancellationToken cancellationToken)
    {
        await using var body = new MemoryStream();
        await file.CopyToAsync(body, cancellationToken);
        var payload = body.ToArray();
        var path = $"/{Uri.EscapeDataString(bucket)}/{Uri.EscapeDataString(key)}";
        using var request = CreateRequest(HttpMethod.Put, path, contentType, payload);
        using var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return new StoredMedia(key, contentType);
    }

    public async Task<(Stream Content, string ContentType)?> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        var path = $"/{Uri.EscapeDataString(bucket)}/{Uri.EscapeDataString(key)}";
        using var request = CreateRequest(HttpMethod.Get, path, "", []);
        var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            response.Dispose();
            return null;
        }
        if (!response.IsSuccessStatusCode)
        {
            response.Dispose();
            throw new HttpRequestException("Object storage could not return the requested media.");
        }
        var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        return (new ResponseOwnedStream(content, response), response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, string contentType, byte[] payload)
    {
        var now = DateTimeOffset.UtcNow;
        var timestamp = now.ToString("yyyyMMdd'T'HHmmss'Z'", System.Globalization.CultureInfo.InvariantCulture);
        var date = now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        var host = endpoint.IsDefaultPort ? endpoint.Host : endpoint.Authority;
        var payloadHash = Hex(SHA256.HashData(payload));
        var canonicalHeaders = $"host:{host}\nx-amz-content-sha256:{payloadHash}\nx-amz-date:{timestamp}\n";
        var signedHeaders = "host;x-amz-content-sha256;x-amz-date";
        var canonicalRequest = $"{method.Method}\n{path}\n\n{canonicalHeaders}{signedHeaders}\n{payloadHash}";
        var scope = $"{date}/{region}/s3/aws4_request";
        var stringToSign = $"AWS4-HMAC-SHA256\n{timestamp}\n{scope}\n{Hex(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)))}";
        var dateKey = Hmac(Encoding.UTF8.GetBytes("AWS4" + secretKey), date);
        var regionKey = Hmac(dateKey, region);
        var serviceKey = Hmac(regionKey, "s3");
        var signingKey = Hmac(serviceKey, "aws4_request");
        var signature = Hex(Hmac(signingKey, stringToSign));
        var request = new HttpRequestMessage(method, new UriBuilder(endpoint) { Path = endpoint.AbsolutePath.TrimEnd('/') + path }.Uri);
        request.Headers.Host = host;
        request.Headers.TryAddWithoutValidation("x-amz-date", timestamp);
        request.Headers.TryAddWithoutValidation("x-amz-content-sha256", payloadHash);
        request.Headers.TryAddWithoutValidation("Authorization", $"AWS4-HMAC-SHA256 Credential={accessKey}/{scope}, SignedHeaders={signedHeaders}, Signature={signature}");
        if (method == HttpMethod.Put)
        {
            request.Content = new ByteArrayContent(payload);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        }
        return request;
    }

    private static byte[] Hmac(byte[] key, string value) => HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(value));
    private static string Hex(byte[] value) => Convert.ToHexString(value).ToLowerInvariant();
    private static string Require(IConfiguration configuration, string key) => configuration[key] is { Length: > 0 } value ? value : throw new InvalidOperationException($"{key} must be configured when Storage:Provider=S3.");

    private sealed class ResponseOwnedStream(Stream content, HttpResponseMessage response) : Stream
    {
        public override bool CanRead => content.CanRead;
        public override bool CanSeek => content.CanSeek;
        public override bool CanWrite => false;
        public override long Length => content.Length;
        public override long Position { get => content.Position; set => content.Position = value; }
        public override void Flush() => content.Flush();
        public override int Read(byte[] buffer, int offset, int count) => content.Read(buffer, offset, count);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => content.ReadAsync(buffer, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => content.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) { content.Dispose(); response.Dispose(); } base.Dispose(disposing); }
        public override async ValueTask DisposeAsync() { await content.DisposeAsync(); response.Dispose(); GC.SuppressFinalize(this); }
    }
}
