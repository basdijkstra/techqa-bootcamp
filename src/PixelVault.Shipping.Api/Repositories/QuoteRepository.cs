using Microsoft.EntityFrameworkCore;
using PixelVault.Shipping.Domain;
using PixelVault.Shipping.Infrastructure;

namespace PixelVault.Shipping.Repositories;

public interface IQuoteRepository
{
    Task<ShippingQuote?> GetAsync(Guid quoteId, CancellationToken cancellationToken = default);

    void Add(ShippingQuote quote);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class QuoteRepository(ShippingDbContext db) : IQuoteRepository
{
    public Task<ShippingQuote?> GetAsync(Guid quoteId, CancellationToken cancellationToken = default) =>
        db.Quotes.Include(q => q.Options).FirstOrDefaultAsync(q => q.Id == quoteId, cancellationToken);

    public void Add(ShippingQuote quote) => db.Quotes.Add(quote);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
}
