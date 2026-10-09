using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PixelVault.Basket.Domain;

namespace PixelVault.Basket.Infrastructure;

public class BasketDbContext(DbContextOptions<BasketDbContext> options) : DbContext(options)
{
    public DbSet<ShoppingBasket> Baskets => Set<ShoppingBasket>();

    public DbSet<BasketItem> BasketItems => Set<BasketItem>();

    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite cannot sort or compare DateTimeOffset values, storing them as numbers fixes that.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShoppingBasket>(basket =>
        {
            basket.ToTable("Baskets");
            basket.HasMany(b => b.Items).WithOne().HasForeignKey(i => i.BasketId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BasketItem>().HasIndex(i => new { i.BasketId, i.GameId }).IsUnique();
        modelBuilder.Entity<PromoCode>().HasIndex(p => p.Code).IsUnique();
    }
}
