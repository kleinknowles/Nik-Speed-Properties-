using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NikSpeed.Api.Data;
using NikSpeed.Api.Data.Entities;
using NikSpeed.Api.Models;
using NikSpeed.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var jwt = builder.Configuration.GetSection("Jwt");
var jwtKey = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key must be configured.");
if (builder.Environment.IsProduction() && (jwtKey.Length < 32 || jwtKey.StartsWith("replace-this", StringComparison.OrdinalIgnoreCase)))
    throw new InvalidOperationException("Set Jwt:Key to a unique random secret of at least 32 characters before running in production.");

builder.Services.AddDbContext<MarketplaceDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("Marketplace")));
builder.Services.AddScoped<TokenService>();
builder.Services.AddHttpClient<FlutterwavePaymentService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => options.TokenValidationParameters = new()
{
    ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
    ValidIssuer = jwt["Issuer"], ValidAudience = jwt["Audience"],
    IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey))
});
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
    database.Database.EnsureCreated();
    database.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS Reservations (Id TEXT NOT NULL PRIMARY KEY, PropertyId TEXT NOT NULL, GuestName TEXT NOT NULL, GuestEmail TEXT NOT NULL, GuestPhone TEXT NOT NULL, CheckIn TEXT NOT NULL, CheckOut TEXT NOT NULL, Guests INTEGER NOT NULL, TotalAmount INTEGER NOT NULL, Status TEXT NOT NULL, CreatedAt TEXT NOT NULL)");
    if (!database.Properties.Any())
    {
        database.Properties.AddRange(SeedProperties());
        database.SaveChanges();
    }
}

app.MapPost("/api/auth/register", async (RegisterRequest request, MarketplaceDbContext database, TokenService tokens) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password) || request.Password.Length < 8)
        return Results.BadRequest(new { message = "Name, email, and a password of at least 8 characters are required." });
    var email = request.Email.Trim().ToLowerInvariant();
    if (await database.Users.AnyAsync(user => user.Email == email)) return Results.Conflict(new { message = "An account with this email already exists." });
    var user = new User { Name = request.Name.Trim(), Email = email, PasswordHash = HashPassword(request.Password) };
    database.Users.Add(user); await database.SaveChangesAsync();
    return Results.Ok(new AuthResponse(tokens.Create(user), user.Name, user.Email));
});

app.MapPost("/api/auth/login", async (LoginRequest request, MarketplaceDbContext database, TokenService tokens) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password)) return Results.Unauthorized();
    var user = await database.Users.SingleOrDefaultAsync(item => item.Email == request.Email.Trim().ToLowerInvariant());
    return user is null || !VerifyPassword(request.Password, user.PasswordHash)
        ? Results.Unauthorized()
        : Results.Ok(new AuthResponse(tokens.Create(user), user.Name, user.Email));
});

app.MapGet("/api/properties", async (MarketplaceDbContext database, string? type, string? location, int? minPrice, int? maxPrice, int? bedrooms, string? sort) =>
{
    var query = database.Properties.AsNoTracking().AsQueryable();
    if (!string.IsNullOrWhiteSpace(type)) query = query.Where(property => property.Type == type);
    if (!string.IsNullOrWhiteSpace(location)) query = query.Where(property => property.Location.Contains(location));
    if (minPrice.HasValue) query = query.Where(property => property.Price >= minPrice);
    if (maxPrice.HasValue) query = query.Where(property => property.Price <= maxPrice);
    if (bedrooms.HasValue) query = query.Where(property => property.Bedrooms >= bedrooms);
    query = sort switch { "price-asc" => query.OrderBy(property => property.Price), "price-desc" => query.OrderByDescending(property => property.Price), _ => query.OrderByDescending(property => property.Featured).ThenByDescending(property => property.CreatedAt) };
    return Results.Ok((await query.ToListAsync()).Select(Map));
});

app.MapGet("/api/properties/{id:guid}", async (Guid id, MarketplaceDbContext database) =>
    await database.Properties.FindAsync(id) is { } property ? Results.Ok(Map(property)) : Results.NotFound());

app.MapPost("/api/properties", async (CreateListingRequest request, ClaimsPrincipal principal, MarketplaceDbContext database) =>
{
    var ownerId = GetUserId(principal); if (ownerId is null) return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Type) || string.IsNullOrWhiteSpace(request.Location) || request.Price <= 0 || request.Area <= 0 || string.IsNullOrWhiteSpace(request.ImageUrl) || string.IsNullOrWhiteSpace(request.Description) || string.IsNullOrWhiteSpace(request.AgentPhone)) return Results.BadRequest(new { message = "Complete the required property details and enter a valid price and area." });
    var paidPlans = await database.Payments.Where(payment => payment.UserId == ownerId && payment.Status == "paid").Select(payment => payment.PlanId).ToListAsync();
    var listingCredits = paidPlans.Sum(planId => ListingPlan.All.SingleOrDefault(plan => plan.Id == planId)?.Listings ?? 0);
    var publishedListings = await database.Properties.CountAsync(property => property.OwnerId == ownerId);
    if (publishedListings >= listingCredits) return Results.StatusCode(StatusCodes.Status402PaymentRequired);
    var user = await database.Users.FindAsync(ownerId);
    var property = new PropertyEntity { OwnerId = ownerId, Title = request.Title, Type = request.Type, Location = request.Location, Price = request.Price, Currency = request.Currency, Bedrooms = request.Bedrooms, Bathrooms = request.Bathrooms, Area = request.Area, AreaUnit = request.AreaUnit, ImageUrl = request.ImageUrl, Description = request.Description, AgentName = user!.Name, AgentPhone = request.AgentPhone };
    database.Properties.Add(property); await database.SaveChangesAsync();
    return Results.Created($"/api/properties/{property.Id}", Map(property));
}).RequireAuthorization();

app.MapPut("/api/properties/{id:guid}", async (Guid id, CreateListingRequest request, ClaimsPrincipal principal, MarketplaceDbContext database) =>
{
    var ownerId = GetUserId(principal); if (ownerId is null) return Results.Unauthorized();
    var property = await database.Properties.SingleOrDefaultAsync(item => item.Id == id && item.OwnerId == ownerId);
    if (property is null) return Results.NotFound(new { message = "Listing not found." });
    if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Location) || request.Price <= 0 || request.Area <= 0)
        return Results.BadRequest(new { message = "A title, location, price, and area are required." });
    property.Title = request.Title.Trim(); property.Type = request.Type.Trim(); property.Location = request.Location.Trim(); property.Price = request.Price;
    property.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "UGX" : request.Currency.Trim(); property.Bedrooms = Math.Max(0, request.Bedrooms); property.Bathrooms = Math.Max(0, request.Bathrooms);
    property.Area = request.Area; property.AreaUnit = string.IsNullOrWhiteSpace(request.AreaUnit) ? "sqm" : request.AreaUnit.Trim(); property.ImageUrl = request.ImageUrl.Trim();
    property.Description = request.Description.Trim(); property.AgentPhone = request.AgentPhone.Trim(); await database.SaveChangesAsync();
    return Results.Ok(Map(property));
}).RequireAuthorization();

app.MapDelete("/api/properties/{id:guid}", async (Guid id, ClaimsPrincipal principal, MarketplaceDbContext database) =>
{
    var ownerId = GetUserId(principal); if (ownerId is null) return Results.Unauthorized();
    var property = await database.Properties.SingleOrDefaultAsync(item => item.Id == id && item.OwnerId == ownerId);
    if (property is null) return Results.NotFound(new { message = "Listing not found." });
    database.Properties.Remove(property); await database.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.MapPost("/api/enquiries", async (EnquiryRequest request, MarketplaceDbContext database) =>
{
    if (!await database.Properties.AnyAsync(property => property.Id == request.PropertyId)) return Results.NotFound(new { message = "Property not found." });
    if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email)) return Results.BadRequest(new { message = "Name and email are required." });
    database.Enquiries.Add(new Enquiry { PropertyId = request.PropertyId, Name = request.Name, Email = request.Email, Phone = request.Phone, Message = request.Message }); await database.SaveChangesAsync();
    return Results.Ok(new { message = "Your enquiry has been sent to the advertiser." });
});

app.MapGet("/api/properties/{id:guid}/availability", async (Guid id, MarketplaceDbContext database) =>
{
    var reservations = await database.Reservations.Where(item => item.PropertyId == id && item.Status != "cancelled")
        .OrderBy(item => item.CheckIn).Select(item => new { item.CheckIn, item.CheckOut }).ToListAsync();
    return Results.Ok(reservations);
});

app.MapPost("/api/reservations", async (CreateReservationRequest request, MarketplaceDbContext database) =>
{
    if (string.IsNullOrWhiteSpace(request.GuestName) || string.IsNullOrWhiteSpace(request.GuestEmail) || request.Guests < 1 || request.CheckIn < DateOnly.FromDateTime(DateTime.UtcNow) || request.CheckOut <= request.CheckIn)
        return Results.BadRequest(new { message = "Enter your contact details, at least one guest, and valid future dates." });
    var property = await database.Properties.FindAsync(request.PropertyId);
    if (property is null) return Results.NotFound(new { message = "Property not found." });
    if (!property.Type.Contains("rent", StringComparison.OrdinalIgnoreCase)) return Results.BadRequest(new { message = "Reservations are available for rental listings only." });
    if (string.IsNullOrWhiteSpace(request.GuestPhone)) return Results.BadRequest(new { message = "A contact phone number is required." });
    var unavailable = await database.Reservations.AnyAsync(item => item.PropertyId == request.PropertyId && item.Status != "cancelled" && request.CheckIn < item.CheckOut && request.CheckOut > item.CheckIn);
    if (unavailable) return Results.Conflict(new { message = "Those dates are no longer available. Please choose different dates." });
    var nights = request.CheckOut.DayNumber - request.CheckIn.DayNumber;
    var reservation = new Reservation { PropertyId = request.PropertyId, GuestName = request.GuestName.Trim(), GuestEmail = request.GuestEmail.Trim().ToLowerInvariant(), GuestPhone = request.GuestPhone.Trim(), CheckIn = request.CheckIn, CheckOut = request.CheckOut, Guests = request.Guests, TotalAmount = property.Price * nights };
    database.Reservations.Add(reservation); await database.SaveChangesAsync();
    return Results.Created($"/api/reservations/{reservation.Id}", new { reservation.Id, reservation.CheckIn, reservation.CheckOut, reservation.Guests, reservation.TotalAmount, reservation.Status });
});

app.MapGet("/api/me/listings", async (ClaimsPrincipal principal, MarketplaceDbContext database) =>
{
    var userId = GetUserId(principal); if (userId is null) return Results.Unauthorized();
    var listings = await database.Properties.Where(property => property.OwnerId == userId).OrderByDescending(property => property.CreatedAt).ToListAsync();
    return Results.Ok(listings.Select(Map));
}).RequireAuthorization();

app.MapGet("/api/me/payments", async (ClaimsPrincipal principal, MarketplaceDbContext database) =>
{
    var userId = GetUserId(principal); if (userId is null) return Results.Unauthorized();
    var payments = await database.Payments.Where(payment => payment.UserId == userId).OrderByDescending(payment => payment.CreatedAt).Select(payment => new { payment.Id, payment.PlanId, payment.Amount, payment.Status, payment.CreatedAt }).ToListAsync();
    return Results.Ok(payments);
}).RequireAuthorization();

app.MapPost("/api/contact", (ContactRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Message))
        return Results.BadRequest(new { message = "Name, email, and message are required." });
    return Results.Ok(new { message = "Thanks for contacting Nik Speed Properties LLC. Our team will reply shortly." });
});

app.MapGet("/api/plans", () => Results.Ok(ListingPlan.All));
app.MapPost("/api/payments/checkout", async (CheckoutRequest request, ClaimsPrincipal principal, MarketplaceDbContext database, FlutterwavePaymentService payments) =>
{
    var userId = GetUserId(principal); if (userId is null) return Results.Unauthorized();
    var plan = ListingPlan.All.SingleOrDefault(item => item.Id == request.PlanId); if (plan is null) return Results.BadRequest(new { message = "Unknown plan." });
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.AdvertiserName)) return Results.BadRequest(new { message = "A billing name and email are required." });
    if (request.PaymentMethod is not ("card" or "mtn" or "airtel")) return Results.BadRequest(new { message = "Choose card, MTN Mobile Money, or Airtel Money." });
    var transaction = new PaymentTransaction { UserId = userId.Value, PlanId = plan.Id, Amount = plan.Price, ProviderReference = $"nikspeed-{userId:N}-{Guid.NewGuid():N}" }; database.Payments.Add(transaction); await database.SaveChangesAsync();
    try
    {
        var checkout = await payments.CreateCheckout(plan, request, transaction.ProviderReference);
        if (checkout.Status is "configuration_required" or "unsupported_payment_method" || string.IsNullOrWhiteSpace(checkout.PaymentLink))
        {
            database.Payments.Remove(transaction);
            await database.SaveChangesAsync();
        }
        return Results.Ok(new { transactionId = transaction.Id, checkout });
    }
    catch (HttpRequestException)
    {
        database.Payments.Remove(transaction);
        await database.SaveChangesAsync();
        return Results.Problem("The payment provider is temporarily unavailable.", statusCode: StatusCodes.Status502BadGateway);
    }
}).RequireAuthorization();

app.MapGet("/api/payments/verify", async (string? tx_ref, string? transaction_id, ClaimsPrincipal principal, MarketplaceDbContext database, FlutterwavePaymentService payments) =>
{
    var userId = GetUserId(principal);
    if (userId is null) return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(tx_ref) || string.IsNullOrWhiteSpace(transaction_id)) return Results.BadRequest(new { message = "The payment reference is incomplete." });
    var payment = await database.Payments.SingleOrDefaultAsync(item => item.UserId == userId && item.ProviderReference == tx_ref);
    if (payment is null) return Results.NotFound(new { message = "Payment not found." });
    if (payment.Status == "paid") return Results.Ok(new { status = payment.Status });
    var verified = await payments.VerifyPayment(transaction_id, payment.ProviderReference, payment.Amount);
    if (verified is null) return Results.Ok(new { status = payment.Status });
    payment.Status = "paid";
    await database.SaveChangesAsync();
    return Results.Ok(new { status = payment.Status });
}).RequireAuthorization();

app.MapPost("/api/payments/flutterwave/webhook", async (HttpRequest request, MarketplaceDbContext database, IConfiguration configuration) =>
{
    var expectedHash = configuration["Flutterwave:WebhookHash"];
    if (string.IsNullOrWhiteSpace(expectedHash) || request.Headers["verif-hash"] != expectedHash) return Results.Unauthorized();
    var payload = await request.ReadFromJsonAsync<FlutterwaveWebhook>();
    if (payload?.Data is null || !string.Equals(payload.Data.Status, "successful", StringComparison.OrdinalIgnoreCase)) return Results.Ok();
    var payment = await database.Payments.SingleOrDefaultAsync(item => item.ProviderReference == payload.Data.TxRef);
    if (payment is null || payment.Amount != payload.Data.Amount || payment.Status == "paid" || !string.Equals(payload.Data.Currency, "UGX", StringComparison.OrdinalIgnoreCase)) return Results.Ok();
    payment.Status = "paid";
    await database.SaveChangesAsync();
    return Results.Ok();
});

app.Run();

static Guid? GetUserId(ClaimsPrincipal principal) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
static string HashPassword(string password) { var salt = RandomNumberGenerator.GetBytes(16); var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 210000, HashAlgorithmName.SHA512, 32); return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}"; }
static bool VerifyPassword(string password, string value) { try { var parts = value.Split('.'); return parts.Length == 2 && CryptographicOperations.FixedTimeEquals(Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(parts[0]), 210000, HashAlgorithmName.SHA512, 32), Convert.FromBase64String(parts[1])); } catch (FormatException) { return false; } }
static Property Map(PropertyEntity item) => new(item.Id, item.Title, item.Type, item.Location, item.Price, item.Currency, item.Bedrooms, item.Bathrooms, item.Area, item.AreaUnit, item.ImageUrl, item.Featured, item.Description, item.AgentName, item.AgentPhone);
static PropertyEntity[] SeedProperties() =>
[
    new() { Title = "Modern villa with Lake Victoria views", Type = "House for sale", Location = "Entebbe, Wakiso", Price = 850000000, Bedrooms = 5, Bathrooms = 4, Area = 450, ImageUrl = "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=1200&q=85", Featured = true, Description = "A light-filled family home in a peaceful, secure estate.", AgentName = "Sarah Namusoke", AgentPhone = "+256 700 123 456" },
    new() { Title = "Serviced two-bedroom apartment", Type = "Apartment for rent", Location = "Kololo, Kampala", Price = 3200000, Currency = "UGX / month", Bedrooms = 2, Bathrooms = 2, Area = 120, ImageUrl = "https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?auto=format&fit=crop&w=1200&q=85", Featured = true, Description = "Contemporary furnished apartment close to shops and restaurants.", AgentName = "Prime Homes", AgentPhone = "+256 772 451 220" },
    new() { Title = "Titled plot near the expressway", Type = "Land", Location = "Kira, Wakiso", Price = 95000000, Bedrooms = 0, Bathrooms = 0, Area = 25, AreaUnit = "decimals", ImageUrl = "https://images.unsplash.com/photo-1500382017468-9049fed747ef?auto=format&fit=crop&w=1200&q=85", Description = "Flat residential land with a ready title and road access.", AgentName = "David Okello", AgentPhone = "+256 758 440 830" }
];

record FlutterwaveWebhook(FlutterwaveWebhookData? Data);
record FlutterwaveWebhookData(string? Status, string? TxRef, int Amount, string? Currency);
record ContactRequest(string Name, string Email, string Message);
