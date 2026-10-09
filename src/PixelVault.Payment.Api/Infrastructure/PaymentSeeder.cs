using System.Security.Cryptography;
using System.Text;
using PixelVault.Payment.Domain;
using PixelVault.Shared.Seed;
using PixelVault.Shared.TestSupport;

namespace PixelVault.Payment.Infrastructure;

/// <summary>Seeds one payment for every seeded order, so refunds work for those orders too.</summary>
public class PaymentSeeder(PaymentDbContext db, TimeProvider timeProvider) : IDataSeeder
{
    // The seeded orders don't store full card numbers, so the fingerprints are made up.
    private static string FakeFingerprint(string last4) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"seed-card-{last4}")));

    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Database.EnsureCreatedAsync(cancellationToken))
        {
            await SeedAsync(cancellationToken);
        }
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureDeletedAsync(cancellationToken);
        await db.Database.EnsureCreatedAsync(cancellationToken);
        await SeedAsync(cancellationToken);
    }

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        foreach (var order in SeedOrders.All)
        {
            var createdAt = order.CreatedAt(now);
            var payment = new PaymentTransaction
            {
                Id = order.PaymentId,
                IdempotencyKey = order.OrderNumber(now),
                OrderReference = order.OrderNumber(now),
                Amount = order.Total,
                Currency = "USD",
                CardBrand = Enum.Parse<CardBrand>(order.CardBrand),
                CardLast4 = order.CardLast4,
                CardFingerprint = FakeFingerprint(order.CardLast4),
                CardholderName = order.Customer.FullName,
                CustomerEmail = order.Customer.Email,
                CreatedAt = createdAt,
                FraudScore = 0,
                Status = order.Status == "PaymentFailed" ? PaymentStatus.Declined : PaymentStatus.Captured,
                DeclineCode = order.DeclineCode
            };

            if (order.Status is "Refunded" or "Cancelled")
            {
                payment.Status = PaymentStatus.Refunded;
                payment.RefundedAmount = payment.Amount;
                payment.Refunds.Add(new Refund
                {
                    Id = Guid.NewGuid(),
                    PaymentId = payment.Id,
                    Amount = payment.Amount,
                    Reason = order.Status == "Cancelled" ? "Order cancelled" : "Customer return",
                    CreatedAt = now.AddDays(-(order.ClosedDaysAgo ?? 0))
                });
            }

            db.Payments.Add(payment);
        }

        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }
}
