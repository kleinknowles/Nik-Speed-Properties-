using Microsoft.EntityFrameworkCore;
using NikSpeed.Api.Data.Entities;

namespace NikSpeed.Api.Data;

public class MarketplaceDbContext(DbContextOptions<MarketplaceDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(user => user.Email).IsUnique().HasFilter("\"Email\" <> ''");
        modelBuilder.Entity<User>().HasIndex(user => user.Phone).IsUnique();
        modelBuilder.Entity<PropertyEntity>().HasIndex(property => property.OwnerId);
        modelBuilder.Entity<PaymentTransaction>().HasIndex(payment => payment.ProviderReference).IsUnique();
        modelBuilder.Entity<Reservation>().HasIndex(reservation => new { reservation.PropertyId, reservation.CheckIn, reservation.CheckOut });
    }

    public override int SaveChanges()
    {
        EnsureEmailIsUniqueForLegacySchema();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureEmailIsUniqueForLegacySchemaAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void EnsureEmailIsUniqueForLegacySchema()
    {
        if (Database.IsSqlite() && ChangeTracker.Entries<User>().Any(entry => entry.State == EntityState.Added))
        {
            var emailInfo = Database.SqlQueryRaw<string>("SELECT name FROM pragma_table_info('Users') WHERE name = 'Phone'").ToArray();
            if (emailInfo.Length == 0)
            {
                Database.ExecuteSqlRaw("ALTER TABLE \"Users\" ADD COLUMN \"Phone\" TEXT NULL");
                Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Users_Email\" ON \"Users\" (\"Email\") WHERE \"Email\" <> ''");
            }
            Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Users_Phone\" ON \"Users\" (\"Phone\") WHERE \"Phone\" IS NOT NULL");
        }
    }

    private async Task EnsureEmailIsUniqueForLegacySchemaAsync(CancellationToken cancellationToken)
    {
        if (Database.IsSqlite() && ChangeTracker.Entries<User>().Any(entry => entry.State == EntityState.Added))
        {
            var hasPhoneColumn = await Database.SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('Users') WHERE name = 'Phone'").AnyAsync(cancellationToken);
            if (!hasPhoneColumn)
            {
                await Database.ExecuteSqlRawAsync("ALTER TABLE \"Users\" ADD COLUMN \"Phone\" TEXT NULL", cancellationToken);
                await Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Users_Email\" ON \"Users\" (\"Email\") WHERE \"Email\" <> ''", cancellationToken);
            }
            await Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Users_Phone\" ON \"Users\" (\"Phone\") WHERE \"Phone\" IS NOT NULL", cancellationToken);
        }
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<PropertyEntity> Properties => Set<PropertyEntity>();
    public DbSet<Enquiry> Enquiries => Set<Enquiry>();
    public DbSet<PaymentTransaction> Payments => Set<PaymentTransaction>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
}
