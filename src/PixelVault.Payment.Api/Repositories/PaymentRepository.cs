using Microsoft.EntityFrameworkCore;
using PixelVault.Payment.Domain;
using PixelVault.Payment.Infrastructure;

namespace PixelVault.Payment.Repositories;

public interface IPaymentRepository
{
    Task<PaymentTransaction?> GetAsync(Guid paymentId, CancellationToken cancellationToken = default);

    Task<PaymentTransaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentTransaction>> GetByOrderReferenceAsync(string orderReference, CancellationToken cancellationToken = default);

    /// <summary>All attempts with the same card since the given moment, used by the fraud check.</summary>
    Task<IReadOnlyList<PaymentTransaction>> GetByCardFingerprintSinceAsync(string fingerprint, DateTimeOffset since, CancellationToken cancellationToken = default);

    void Add(PaymentTransaction payment);

    void AddRefund(Refund refund);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class PaymentRepository(PaymentDbContext db) : IPaymentRepository
{
    public Task<PaymentTransaction?> GetAsync(Guid paymentId, CancellationToken cancellationToken = default) =>
        db.Payments.Include(p => p.Refunds).FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

    public Task<PaymentTransaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        db.Payments.Include(p => p.Refunds).FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<IReadOnlyList<PaymentTransaction>> GetByOrderReferenceAsync(string orderReference, CancellationToken cancellationToken = default) =>
        await db.Payments.Include(p => p.Refunds)
            .Where(p => p.OrderReference == orderReference)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PaymentTransaction>> GetByCardFingerprintSinceAsync(string fingerprint, DateTimeOffset since, CancellationToken cancellationToken = default) =>
        await db.Payments
            .Where(p => p.CardFingerprint == fingerprint && p.CreatedAt >= since)
            .ToListAsync(cancellationToken);

    public void Add(PaymentTransaction payment) => db.Payments.Add(payment);

    public void AddRefund(Refund refund) => db.Refunds.Add(refund);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
}
