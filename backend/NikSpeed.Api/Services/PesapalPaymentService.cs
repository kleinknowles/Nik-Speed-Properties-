using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using NikSpeed.Api.Models;

namespace NikSpeed.Api.Services;

public sealed class PesapalPaymentService(HttpClient client, IConfiguration configuration)
{
    private IConfigurationSection Settings => configuration.GetSection("Pesapal");

    public async Task<CheckoutResult> CreateCheckout(ListingPlan plan, CheckoutRequest request, string merchantReference)
    {
        var settings = Settings;
        if (string.IsNullOrWhiteSpace(settings["ConsumerKey"]) || string.IsNullOrWhiteSpace(settings["ConsumerSecret"]) || !Guid.TryParse(settings["NotificationId"], out _))
            return new CheckoutResult(merchantReference, null, "configuration_required");

        var token = await RequestToken();
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/Transactions/SubmitOrderRequest");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        message.Content = JsonContent.Create(new
        {
            id = merchantReference,
            currency = "UGX",
            amount = plan.Price,
            description = $"{plan.Name} property listing package",
            callback_url = GetCallbackUrl(),
            cancellation_url = GetCallbackUrl(),
            notification_id = settings["NotificationId"],
            billing_address = new { email_address = request.Email, country_code = "UG", first_name = request.AdvertiserName }
        });

        using var response = await client.SendAsync(message);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<SubmitOrderResponse>();
        if (payload is null || !string.Equals(payload.Status, "200", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(payload.RedirectUrl))
            throw new HttpRequestException("Pesapal did not return a checkout URL.");
        return new CheckoutResult(merchantReference, payload.RedirectUrl, "pending");
    }

    public async Task<VerifiedPayment?> VerifyPayment(string orderTrackingId, string expectedReference, int expectedAmount)
    {
        if (string.IsNullOrWhiteSpace(orderTrackingId) || string.IsNullOrWhiteSpace(Settings["ConsumerKey"]) || string.IsNullOrWhiteSpace(Settings["ConsumerSecret"])) return null;
        var token = await RequestToken();
        var url = $"api/Transactions/GetTransactionStatus?orderTrackingId={Uri.EscapeDataString(orderTrackingId)}";
        using var message = new HttpRequestMessage(HttpMethod.Get, url);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(message);
        if (!response.IsSuccessStatusCode) return null;
        var result = await response.Content.ReadFromJsonAsync<TransactionStatusResponse>();
        if (result is null || !string.Equals(result.Status, "200", StringComparison.Ordinal)
            || !string.Equals(result.PaymentStatusDescription, "COMPLETED", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(result.MerchantReference, expectedReference, StringComparison.Ordinal)
            || !string.Equals(result.Currency, "UGX", StringComparison.OrdinalIgnoreCase)
            || result.Amount < expectedAmount) return null;
        return new VerifiedPayment(orderTrackingId, result.MerchantReference!, result.Amount, result.Currency!);
    }

    private async Task<string> RequestToken()
    {
        ConfigureClient();
        using var response = await client.PostAsJsonAsync("api/Auth/RequestToken", new
        {
            consumer_key = Settings["ConsumerKey"],
            consumer_secret = Settings["ConsumerSecret"]
        });
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        if (string.IsNullOrWhiteSpace(token?.Token)) throw new HttpRequestException("Pesapal authentication did not return a token.");
        return token.Token;
    }

    private string GetCallbackUrl()
    {
        var configured = Settings["CallbackUrl"];
        if (!string.IsNullOrWhiteSpace(configured)) return configured;
        var origin = configuration["WebOrigin"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(origin)) throw new InvalidOperationException("Set WebOrigin or Pesapal:CallbackUrl before creating checkout.");
        if (!origin.Contains("://", StringComparison.Ordinal)) origin = $"https://{origin}";
        return $"{origin}/payment/complete";
    }

    private HttpClient ConfigureClient()
    {
        var baseUrl = Settings["BaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl)) throw new InvalidOperationException("Set Pesapal:BaseUrl.");
        client.BaseAddress = new Uri($"{baseUrl}/", UriKind.Absolute);
        return client;
    }

    private sealed record TokenResponse([property: JsonPropertyName("token")] string? Token);
    private sealed record SubmitOrderResponse([property: JsonPropertyName("redirect_url")] string? RedirectUrl, [property: JsonPropertyName("status")] string? Status);
    private sealed record TransactionStatusResponse(
        [property: JsonPropertyName("payment_status_description")] string? PaymentStatusDescription,
        [property: JsonPropertyName("merchant_reference")] string? MerchantReference,
        [property: JsonPropertyName("amount")] decimal Amount,
        [property: JsonPropertyName("currency")] string? Currency,
        [property: JsonPropertyName("status")] string? Status);
}

public record CheckoutResult(string TransactionReference, string? PaymentLink, string Status);
public record VerifiedPayment(string? TransactionId, string TransactionReference, decimal Amount, string Currency);
