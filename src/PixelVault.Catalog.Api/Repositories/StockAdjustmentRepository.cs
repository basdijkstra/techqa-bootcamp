using Microsoft.EntityFrameworkCore;
using PixelVault.Catalog.Domain;
using PixelVault.Catalog.Infrastructure;

namespace PixelVault.Catalog.Repositories;

public interface IStockAdjustmentRepository
{
    /// <summary>Adds the adjustment. It is saved together with the game by <see cref="IGameRepository.SaveChangesAsync"/>.</summary>
    void Add(StockAdjustment adjustment);

    Task<IReadOnlyList<StockAdjustment>> GetForGameAsync(int gameId, CancellationToken cancellationToken = default);
}

public class StockAdjustmentRepository(CatalogDbContext db) : IStockAdjustmentRepository
{
    public void Add(StockAdjustment adjustment) => db.StockAdjustments.Add(adjustment);

    public async Task<IReadOnlyList<StockAdjustment>> GetForGameAsync(int gameId, CancellationToken cancellationToken = default) =>
        await db.StockAdjustments
            .Where(a => a.GameId == gameId)
            .OrderByDescending(a => a.PerformedAt)
            .ThenByDescending(a => a.Id)
            .ToListAsync(cancellationToken);
}
