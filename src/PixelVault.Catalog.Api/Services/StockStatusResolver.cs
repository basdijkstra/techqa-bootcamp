using PixelVault.Catalog.Domain;

namespace PixelVault.Catalog.Services;

public interface IStockStatusResolver
{
    StockStatus Resolve(Game game, DateOnly today);
}

/// <summary>
/// Stock status rules, checked in this order:
/// <list type="number">
/// <item>Release date after today: <see cref="StockStatus.PreOrder"/>, whatever the stock.</item>
/// <item>No units: <see cref="StockStatus.OutOfStock"/>.</item>
/// <item>1 to 5 units: <see cref="StockStatus.LowStock"/>.</item>
/// <item>More than 5: <see cref="StockStatus.InStock"/>.</item>
/// </list>
/// </summary>
public class StockStatusResolver : IStockStatusResolver
{
    public const int LowStockThreshold = 5;

    public StockStatus Resolve(Game game, DateOnly today)
    {
        if (game.ReleaseDate > today)
        {
            return StockStatus.PreOrder;
        }

        if (game.StockQuantity <= 0)
        {
            return StockStatus.OutOfStock;
        }

        return game.StockQuantity <= LowStockThreshold ? StockStatus.LowStock : StockStatus.InStock;
    }
}
