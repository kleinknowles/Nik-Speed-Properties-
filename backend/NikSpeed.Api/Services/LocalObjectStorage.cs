using System.Text.RegularExpressions;

namespace NikSpeed.Api.Services;

public sealed class LocalObjectStorage(IWebHostEnvironment environment, IConfiguration configuration) : IObjectStorage
{
    private string Root => Path.GetFullPath(configuration["Storage:UploadPath"] ?? Path.Combine(environment.ContentRootPath, "App_Data", "uploads"));

    public async Task<StoredMedia> SaveAsync(IFormFile file, string key, string contentType, CancellationToken cancellationToken)
    {
        var path = GetPath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await file.CopyToAsync(output, cancellationToken);
        return new StoredMedia(key, contentType);
    }

    public Task<(Stream Content, string ContentType)?> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        var path = GetPath(key);
        if (!File.Exists(path)) return Task.FromResult<(Stream Content, string ContentType)?>(null);
        var contentType = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp",
            ".mp4" => "video/mp4", ".mov" => "video/quicktime", ".webm" => "video/webm", _ => "application/octet-stream"
        };
        return Task.FromResult<(Stream Content, string ContentType)?>( (File.OpenRead(path), contentType) );
    }

    private string GetPath(string key)
    {
        if (!Regex.IsMatch(key, @"\A[A-Za-z0-9_-]+\.(jpg|jpeg|png|webp|mp4|mov|webm)\z", RegexOptions.IgnoreCase))
            throw new ArgumentException("Invalid media object key.", nameof(key));
        return Path.Combine(Root, key);
    }
}
