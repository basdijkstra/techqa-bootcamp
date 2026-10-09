using PixelVault.Shared.Web;

namespace PixelVault.Basket.Services;

/// <summary>A basket line with its current price, as used by the discount calculators.</summary>
public record PricedLine(int GameId, string Category, bool IsPreOrder, int Quantity, decimal UnitPrice)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

public record BundleDiscountResult(decimal Discount, IReadOnlyDictionary<string, decimal> DiscountPerCategory);

public interface IBundleDiscountCalculator
{
    BundleDiscountResult Calculate(IEnumerable<PricedLine> lines);
}

/// <summary>
/// "Buy 3, get 50% off the cheapest": for every 3 units in the same category, the cheapest unit is half price.
/// <list type="bullet">
/// <item>Counted per unit, so 3 copies of the same game also qualify.</item>
/// <item>6 units in one category give 2 half-price units, 5 units give 1.</item>
/// <item>Pre-orders don't count and are never discounted.</item>
/// <item>Each discounted unit is rounded to cents, halves away from zero.</item>
/// </list>
/// </summary>
public class BundleDiscountCalculator : IBundleDiscountCalculator
{
    public const int UnitsPerBundle = 3;
    public const decimal BundleDiscountRate = 0.5m;

    public BundleDiscountResult Calculate(IEnumerable<PricedLine> lines)
    {
        var perCategory = new Dictionary<string, decimal>();

        var groups = lines
            .Where(l => !l.IsPreOrder && l.Quantity > 0)
            .GroupBy(l => l.Category);

        foreach (var group in groups)
        {
            var unitPrices = group
                .SelectMany(l => Enumerable.Repeat(l.UnitPrice, l.Quantity))
                .OrderBy(price => price)
                .ToList();

            var discountedUnits = unitPrices.Count / UnitsPerBundle;
            if (discountedUnits == 0)
            {
                continue;
            }

            var discount = unitPrices
                .Take(discountedUnits)
                .Sum(price => MoneyMath.RoundToCents(price * BundleDiscountRate));

            perCategory[group.Key] = discount;
        }

        return new BundleDiscountResult(perCategory.Values.Sum(), perCategory);
    }
}
