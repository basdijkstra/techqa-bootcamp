using PixelVault.Catalog.Domain;
using PixelVault.Catalog.Dtos;
using PixelVault.Shared.Web;

namespace PixelVault.Catalog.Services;

public interface IPricingCalculator
{
    /// <summary>Works out the price a customer pays for a game at the given moment.</summary>
    PriceDto Calculate(Game game, DateTimeOffset now);

    bool IsSaleActive(Game game, DateTimeOffset now);
}

/// <summary>
/// Pricing rules:
/// <list type="bullet">
/// <item>A sale applies when the percentage is between 1 and 90 and <c>now</c> is on or after the start and before the end.</item>
/// <item>The sale price is rounded to whole cents, halves away from zero.</item>
/// <item>A sale never takes the price below the price floor of $4.99. Games that cost less than that are never discounted.</item>
/// </list>
/// </summary>
public class PricingCalculator : IPricingCalculator
{
    public const decimal PriceFloor = 4.99m;
    public const int MinSalePercentage = 1;
    public const int MaxSalePercentage = 90;

    public bool IsSaleActive(Game game, DateTimeOffset now)
    {
        if (game.SalePercentage is not { } percentage || percentage < MinSalePercentage || percentage > MaxSalePercentage)
        {
            return false;
        }

        if (game.SaleStartsAt is null || now < game.SaleStartsAt)
        {
            return false;
        }

        return game.SaleEndsAt is null || now < game.SaleEndsAt;
    }

    public PriceDto Calculate(Game game, DateTimeOffset now)
    {
        if (!IsSaleActive(game, now))
        {
            return new PriceDto(game.BasePrice, game.BasePrice, false, null, 0m, null);
        }

        var percentage = game.SalePercentage!.Value;
        var discounted = MoneyMath.RoundToCents(game.BasePrice * (100 - percentage) / 100m);
        var floor = Math.Min(PriceFloor, game.BasePrice);
        var price = Math.Max(discounted, floor);

        if (price == game.BasePrice)
        {
            return new PriceDto(game.BasePrice, game.BasePrice, false, null, 0m, null);
        }

        return new PriceDto(game.BasePrice, price, true, percentage, game.BasePrice - price, game.SaleEndsAt);
    }
}
