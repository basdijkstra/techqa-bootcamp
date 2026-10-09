using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PixelVault.Payment.Domain;

namespace PixelVault.Payment.Infrastructure;

public class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<PaymentTransaction> Payments => Set<PaymentTransaction>();

    public DbSet<Refund> Refunds => Set<Refund>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite cannot sort or compare DateTimeOffset values, storing them as numbers fixes that.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentTransaction>(payment =>
        {
            payment.ToTable("Payments");
            payment.HasIndex(p => p.IdempotencyKey).IsUnique();
            payment.HasIndex(p => p.OrderReference);
            payment.HasIndex(p => p.CardFingerprint);
            payment.HasMany(p => p.Refunds).WithOne().HasForeignKey(r => r.PaymentId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
