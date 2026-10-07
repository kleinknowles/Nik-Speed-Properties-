namespace NikSpeed.Api.Models;

public record RegisterRequest(string Name, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string Name, string Email);
public record CreateListingRequest(
    string Title, string Type, string Location, int Price, string Currency,
    int Bedrooms, int Bathrooms, int Area, string AreaUnit, string ImageUrl,
    string Description, string AgentPhone);

public record CreateReservationRequest(Guid PropertyId, string GuestName, string GuestEmail, string GuestPhone, DateOnly CheckIn, DateOnly CheckOut, int Guests);
