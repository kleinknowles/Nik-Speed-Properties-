using NikSpeed.Api.Models;

namespace NikSpeed.Api.Services;

public class PropertyRepository
{
    private readonly List<Property> properties =
    [
        new(Guid.NewGuid(), "Modern villa with Lake Victoria views", "House for sale", "Entebbe, Wakiso", 850000000, "UGX", 5, 4, 450, "sqm", "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=1200&q=85", true, "A light-filled family home in a peaceful, secure estate.", "Sarah Namusoke", "+256 700 123 456"),
        new(Guid.NewGuid(), "Serviced two-bedroom apartment", "Apartment for rent", "Kololo, Kampala", 3200000, "UGX / month", 2, 2, 120, "sqm", "https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?auto=format&fit=crop&w=1200&q=85", true, "Contemporary furnished apartment close to shops and restaurants.", "Prime Homes", "+256 772 451 220"),
        new(Guid.NewGuid(), "Titled plot near the expressway", "Land", "Kira, Wakiso", 95000000, "UGX", 0, 0, 25, "decimals", "https://images.unsplash.com/photo-1500382017468-9049fed747ef?auto=format&fit=crop&w=1200&q=85", false, "Flat residential land with a ready title and road access.", "David Okello", "+256 758 440 830"),
        new(Guid.NewGuid(), "Three-bedroom family home", "House for sale", "Ntinda, Kampala", 640000000, "UGX", 3, 3, 280, "sqm", "https://images.unsplash.com/photo-1600566753086-00f18fb6b3ea?auto=format&fit=crop&w=1200&q=85", false, "Well-kept home with mature garden and staff quarters.", "Urban Key", "+256 704 900 100")
    ];

    public IEnumerable<Property> Search(string? type, string? location, int? minPrice, int? maxPrice, int? bedrooms, string? sort) => properties
        .Where(property => string.IsNullOrWhiteSpace(type) || property.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
        .Where(property => string.IsNullOrWhiteSpace(location) || property.Location.Contains(location, StringComparison.OrdinalIgnoreCase))
        .Where(property => !minPrice.HasValue || property.Price >= minPrice)
        .Where(property => !maxPrice.HasValue || property.Price <= maxPrice)
        .Where(property => !bedrooms.HasValue || property.Bedrooms >= bedrooms)
        .OrderByDescending(property => sort == "price-asc" ? false : property.Featured)
        .ThenBy(property => sort == "price-asc" ? property.Price : sort == "price-desc" ? -property.Price : 0);

    public Property? Find(Guid id) => properties.SingleOrDefault(property => property.Id == id);

    public Property Add(CreatePropertyRequest request)
    {
        var property = new Property(Guid.NewGuid(), request.Title, request.Type, request.Location, request.Price,
            request.Currency, request.Bedrooms, request.Bathrooms, request.Area, request.AreaUnit, request.ImageUrl,
            false, request.Description, request.AgentName, request.AgentPhone);
        properties.Add(property);
        return property;
    }
}
