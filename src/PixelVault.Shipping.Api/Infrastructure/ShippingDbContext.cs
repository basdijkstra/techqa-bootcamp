using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PixelVault.Shipping.Domain;
using PixelVault.Shared.TestSupport;

namespace PixelVault.Shipping.Infrastructure;

public class ShippingDbContext(DbContextOptions<ShippingDbContext> options) : DbContext(options)
{
    public DbSet<ShippingQuote> Quotes => Set<ShippingQuote>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite cannot sort or compare DateTimeOffset values, storing them as numbers fixes that.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShippingQuote>()
            .HasMany(q => q.Options)
            .WithOne()
            .HasForeignKey(o => o.QuoteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Shipping has no seed data; quotes are created during checkout.</summary>
public class ShippingSeeder(ShippingDbContext db) : IDataSeeder
{
    public Task EnsureSeededAsync(CancellationToken cancellationToken = default) =>
        db.Database.EnsureCreatedAsync(cancellationToken);

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureDeletedAsync(cancellationToken);
        await db.Database.EnsureCreatedAsync(cancellationToken);
    }
}
