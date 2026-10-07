using System.Net.Http.Headers;
using System.Net.Http.Json;
using NikSpeed.Api.Models;

namespace NikSpeed.Api.Services;

public class FlutterwavePaymentService(HttpClient client, IConfiguration configuration)
{
    private IConfigurationSection Settings => configuration.GetSection("Flutterwave");

    public async Task<CheckoutResult> CreateCheckout(ListingPlan plan, CheckoutRequest request, string transactionReference)
    {
        var settings = Settings;
        var secretKey = settings["SecretKey"];
        var paymentOption = (request.PaymentMethod ?? "card").Trim().ToLowerInvariant() switch
        {
            "card" => "card",
            "mtn" or "airtel" or "mobilemoney" => "mobilemoneyuganda",
            _ => ""
        };

        if (string.IsNullOrWhiteSpace(paymentOption))
            return new CheckoutResult(transactionReference, null, "unsupported_payment_method");

        if (string.IsNullOrWhiteSpace(secretKey))
            return new CheckoutResult(transactionReference, null, "configuration_required");

        client.BaseAddress = new Uri(settings["BaseUrl"]!);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);
        var response = await client.PostAsJsonAsync("payments", new
        {
            tx_ref = transactionReference,
            amount = plan.Price,
            currency = "UGX",
            payment_options = paymentOption,
            redirect_url = settings["RedirectUrl"],
            customer = new { email = request.Email, name = request.AdvertiserName },
            customizations = new { title = "Nik Speed Properties LLC", description = $"{plan.Name} listing package" }
        });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<FlutterwaveResponse>();
        return new CheckoutResult(transactionReference, payload?.Data?.Link, "pending");
    }

    public async Task<VerifiedPayment?> VerifyPayment(string transactionId, string expectedReference, int expectedAmount)
    {
        var settings = Settings;
        var secretKey = settings["SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey)) return null;
        client.BaseAddress = new Uri(settings["BaseUrl"]!);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);
        using var response = await client.GetAsync($"transactions/{Uri.EscapeDataString(transactionId)}/verify");
        if (!response.IsSuccessStatusCode) return null;
        var payload = await response.Content.ReadFromJsonAsync<VerificationResponse>();
        var data = payload?.Data;
        if (data is null || !string.Equals(data.Status, "successful", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(data.TxRef, expectedReference, StringComparison.Ordinal)
            || !string.Equals(data.Currency, "UGX", StringComparison.OrdinalIgnoreCase)
            || data.Amount < expectedAmount) return null;
        return new VerifiedPayment(data.Id, data.TxRef!, data.Amount, data.Currency!);
    }

    private record FlutterwaveResponse(FlutterwaveData? Data);
    private record FlutterwaveData(string? Link);
    private record VerificationResponse(VerificationData? Data);
    private record VerificationData(string? Status, string? TxRef, int Amount, string? Currency, string? Id);
}

public record CheckoutResult(string TransactionReference, string? PaymentLink, string Status);
public record VerifiedPayment(string? TransactionId, string TransactionReference, int Amount, string Currency);
