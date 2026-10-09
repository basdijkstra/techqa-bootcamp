using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PixelVault.Orders.Domain;

namespace PixelVault.Orders.Infrastructure;

public class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite cannot sort or compare DateTimeOffset values, storing them as numbers fixes that.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(order =>
        {
            order.HasIndex(o => o.OrderNumber).IsUnique();
            order.HasIndex(o => o.CustomerEmail);
            order.HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OrderId).OnDelete(DeleteBehavior.Cascade);
            order.HasMany(o => o.History).WithOne().HasForeignKey(h => h.OrderId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
