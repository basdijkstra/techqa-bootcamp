using Microsoft.EntityFrameworkCore;
using PixelVault.Basket.Domain;
using PixelVault.Basket.Infrastructure;

namespace PixelVault.Basket.Repositories;

public interface IPromoCodeRepository
{
    /// <summary>Gets a promo code by its code (case-insensitive), or null.</summary>
    Task<PromoCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}

public class PromoCodeRepository(BasketDbContext db) : IPromoCodeRepository
{
    public Task<PromoCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return db.PromoCodes.FirstOrDefaultAsync(p => p.Code == normalized, cancellationToken);
    }
}
