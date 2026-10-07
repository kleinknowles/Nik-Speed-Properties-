using NikSpeed.Api.Models;

namespace NikSpeed.Api.Services;

public class PaymentService
{
    public object CreateCheckout(ListingPlan plan, CheckoutRequest request) => new
    {
        checkoutId = Guid.NewGuid(),
        plan = plan.Name,
        amount = plan.Price,
        currency = "UGX",
        status = "pending",
        message = $"Checkout created for {request.AdvertiserName}. Connect this endpoint to a payment provider to collect payment."
    };
}
