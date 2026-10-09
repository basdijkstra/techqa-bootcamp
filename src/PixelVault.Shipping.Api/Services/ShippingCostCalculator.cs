using PixelVault.Shipping.Domain;
using PixelVault.Shared.Errors;

namespace PixelVault.Shipping.Services;

public interface IShippingZoneResolver
{
    ShippingZone Resolve(string country, string region);
}

/// <summary>Canada is one zone. Alaska and Hawaii are their own zone. Every other state (and DC) is contiguous.</summary>
public class ShippingZoneResolver : IShippingZoneResolver
{
    public ShippingZone Resolve(string country, string region) => country switch
    {
        Countries.Canada => ShippingZone.Canada,
        Countries.UnitedStates when region is "AK" or "HI" => ShippingZone.AlaskaHawaii,
        Countries.UnitedStates => ShippingZone.ContiguousUS,
        _ => throw new ArgumentOutOfRangeException(nameof(country), country, "Unsupported country.")
    };
}

public record ShippingCost(decimal Cost, bool IsFree);

public interface IShippingCostCalculator
{
    IReadOnlyList<ShippingMethod> AvailableMethods(ShippingZone zone);

    ShippingCost Calculate(ShippingZone zone, ShippingMethod method, int itemCount, decimal orderSubtotal);
}

/// <summary>
/// Shipping rates:
/// <code>
/// Zone            Standard   Express    Extra per item after the 3rd (Standard / Express)
/// Contiguous US    $5.99     $14.99     $1.00 / $2.00
/// Alaska/Hawaii   $12.99     $29.99     $1.00 / $2.00
/// Canada          $15.99     n/a        $1.00
/// </code>
/// Standard shipping is free in the contiguous US when the order subtotal (after discounts) is $50.00 or more.
/// Express is not available to Canada.
/// </summary>
public class ShippingCostCalculator : IShippingCostCalculator
{
    public const decimal FreeShippingThreshold = 50.00m;
    public const int ItemsIncludedInBaseRate = 3;

    private static readonly Dictionary<(ShippingZone, ShippingMethod), decimal> BaseRates = new()
    {
        [(ShippingZone.ContiguousUS, ShippingMethod.Standard)] = 5.99m,
        [(ShippingZone.ContiguousUS, ShippingMethod.Express)] = 14.99m,
        [(ShippingZone.AlaskaHawaii, ShippingMethod.Standard)] = 12.99m,
        [(ShippingZone.AlaskaHawaii, ShippingMethod.Express)] = 29.99m,
        [(ShippingZone.Canada, ShippingMethod.Standard)] = 15.99m
    };

    private static readonly Dictionary<ShippingMethod, decimal> ExtraItemRates = new()
    {
        [ShippingMethod.Standard] = 1.00m,
        [ShippingMethod.Express] = 2.00m
    };

    public IReadOnlyList<ShippingMethod> AvailableMethods(ShippingZone zone) =>
        Enum.GetValues<ShippingMethod>().Where(m => BaseRates.ContainsKey((zone, m))).ToList();

    public ShippingCost Calculate(ShippingZone zone, ShippingMethod method, int itemCount, decimal orderSubtotal)
    {
        if (itemCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(itemCount), itemCount, "At least one item is required.");
        }

        if (!BaseRates.TryGetValue((zone, method), out var baseRate))
        {
            throw new BusinessRuleException("SHIPPING_METHOD_UNAVAILABLE", $"{method} shipping is not available for {zone}.");
        }

        if (zone == ShippingZone.ContiguousUS && method == ShippingMethod.Standard && orderSubtotal >= FreeShippingThreshold)
        {
            return new ShippingCost(0m, true);
        }

        var extraItems = Math.Max(0, itemCount - ItemsIncludedInBaseRate);
        return new ShippingCost(baseRate + extraItems * ExtraItemRates[method], false);
    }
}
