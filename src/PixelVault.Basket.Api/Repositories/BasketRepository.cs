using Microsoft.EntityFrameworkCore;
using PixelVault.Basket.Domain;
using PixelVault.Basket.Infrastructure;

namespace PixelVault.Basket.Repositories;

public interface IBasketRepository
{
    /// <summary>Gets a basket including its items, or null.</summary>
    Task<ShoppingBasket?> GetAsync(Guid basketId, CancellationToken cancellationToken = default);

    void Add(ShoppingBasket basket);

    void AddItem(BasketItem item);

    void RemoveItem(BasketItem item);

    /// <summary>Saves all pending changes, including changes to promo codes.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class BasketRepository(BasketDbContext db) : IBasketRepository
{
    public Task<ShoppingBasket?> GetAsync(Guid basketId, CancellationToken cancellationToken = default) =>
        db.Baskets.Include(b => b.Items).FirstOrDefaultAsync(b => b.Id == basketId, cancellationToken);

    public void Add(ShoppingBasket basket) => db.Baskets.Add(basket);

    public void AddItem(BasketItem item) => db.BasketItems.Add(item);

    public void RemoveItem(BasketItem item) => db.BasketItems.Remove(item);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
}
