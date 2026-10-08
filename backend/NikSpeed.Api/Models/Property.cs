namespace NikSpeed.Api.Models;

public record Property(
    Guid Id,
    string Title,
    string Type,
    string Location,
    int Price,
    string Currency,
    int Bedrooms,
    int Bathrooms,
    int Area,
    string AreaUnit,
    string ImageUrl,
    bool Featured,
    string Description,
    string AgentName,
    string AgentPhone,
    string VideoUrl = "",
    string[]? ImageUrls = null,
    string[]? VideoUrls = null);

public record CreatePropertyRequest(
    string Title, string Type, string Location, int Price, string Currency,
    int Bedrooms, int Bathrooms, int Area, string AreaUnit, string ImageUrl,
    string Description, string AgentName, string AgentPhone, string VideoUrl = "");

public record CheckoutRequest(string PlanId, string AdvertiserName, string Email, string PaymentMethod = "card");

public record EnquiryRequest(Guid PropertyId, string Name, string Email, string Phone, string Message);

public record ListingPlan(string Id, string Name, int Price, int Listings, bool Featured)
{
    public static readonly ListingPlan[] All =
    [
        new("starter", "Starter", 25000, 1, false),
        new("professional", "Professional", 75000, 5, true),
        new("agency", "Agency", 180000, 15, true)
    ];
}
