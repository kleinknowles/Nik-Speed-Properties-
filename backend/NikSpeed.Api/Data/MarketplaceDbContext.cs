using Microsoft.EntityFrameworkCore;
using NikSpeed.Api.Data.Entities;

namespace NikSpeed.Api.Data;

public class MarketplaceDbContext(DbContextOptions<MarketplaceDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<PropertyEntity> Properties => Set<PropertyEntity>();
    public DbSet<Enquiry> Enquiries => Set<Enquiry>();
    public DbSet<PaymentTransaction> Payments => Set<PaymentTransaction>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
}
