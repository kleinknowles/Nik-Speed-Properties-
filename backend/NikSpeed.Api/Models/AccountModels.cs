namespace NikSpeed.Api.Models;

public record RegisterRequest(string Name, string Email, string Password, string? Phone = null);
public record LoginRequest(string Email, string Password, string? Phone = null);
public record AuthResponse(string Token, string Name, string Email, string? Phone = null);
public record CreateListingRequest(
    string Title, string Type, string Location, int Price, string Currency,
    int Bedrooms, int Bathrooms, int Area, string AreaUnit, string ImageUrl,
    string Description, string AgentPhone, string VideoUrl = "",
    string[]? ImageUrls = null, string[]? VideoUrls = null);

public record CreateReservationRequest(Guid PropertyId, string GuestName, string GuestEmail, string GuestPhone, DateOnly CheckIn, DateOnly CheckOut, int Guests);
