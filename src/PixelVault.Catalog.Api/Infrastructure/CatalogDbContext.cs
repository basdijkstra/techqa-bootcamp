using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PixelVault.Catalog.Domain;

namespace PixelVault.Catalog.Infrastructure;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Game> Games => Set<Game>();

    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();

    public DbSet<StockReservation> StockReservations => Set<StockReservation>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite cannot sort or compare DateTimeOffset values, storing them as numbers fixes that.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>(game =>
        {
            game.HasIndex(g => g.Slug).IsUnique();
            game.Property(g => g.Title).HasMaxLength(100);
            game.Property(g => g.BasePrice).HasPrecision(10, 2);
        });

        modelBuilder.Entity<StockAdjustment>().HasIndex(a => a.GameId);
        modelBuilder.Entity<StockReservation>().HasIndex(r => r.OrderReference);
    }
}
