using PixelVault.Shipping.Domain;

namespace PixelVault.Shipping.Services;

public record DeliveryEstimate(DateOnly ShipDate, DateOnly DeliveryDate, int TransitBusinessDays);

public interface IDeliveryDateEstimator
{
    DeliveryEstimate Estimate(ShippingZone zone, ShippingMethod method, DateTimeOffset orderPlacedAt, DateOnly? shipNotBefore = null);

    bool IsBusinessDay(DateOnly date);
}

/// <summary>
/// Delivery date rules. The warehouse is in New York, so all times are US Eastern time.
/// <list type="bullet">
/// <item>Business days are Monday to Friday, except US federal holidays (observed dates).</item>
/// <item>Orders placed on a business day before 2:00 PM ship the same day. Later orders, and orders placed on a
/// weekend or holiday, ship on the next business day.</item>
/// <item>Orders with pre-orders ship on the first business day on or after the release date
/// (<c>shipNotBefore</c>), if that is later.</item>
/// <item>Delivery is a number of business days after shipping:
/// contiguous US 5 (Standard) or 2 (Express), Alaska/Hawaii 8 or 3, Canada 10.</item>
/// </list>
/// </summary>
public class DeliveryDateEstimator(IHolidayCalendar holidays) : IDeliveryDateEstimator
{
    public static readonly TimeOnly CutoffTime = new(14, 0);

    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    private static readonly Dictionary<(ShippingZone, ShippingMethod), int> TransitDays = new()
    {
        [(ShippingZone.ContiguousUS, ShippingMethod.Standard)] = 5,
        [(ShippingZone.ContiguousUS, ShippingMethod.Express)] = 2,
        [(ShippingZone.AlaskaHawaii, ShippingMethod.Standard)] = 8,
        [(ShippingZone.AlaskaHawaii, ShippingMethod.Express)] = 3,
        [(ShippingZone.Canada, ShippingMethod.Standard)] = 10
    };

    public bool IsBusinessDay(DateOnly date) =>
        date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !holidays.IsHoliday(date);

    public DeliveryEstimate Estimate(ShippingZone zone, ShippingMethod method, DateTimeOffset orderPlacedAt, DateOnly? shipNotBefore = null)
    {
        if (!TransitDays.TryGetValue((zone, method), out var transitDays))
        {
            throw new ArgumentException($"{method} shipping is not available for {zone}.", nameof(method));
        }

        var local = TimeZoneInfo.ConvertTime(orderPlacedAt, Eastern);
        var orderDate = DateOnly.FromDateTime(local.DateTime);
        var orderTime = TimeOnly.FromDateTime(local.DateTime);

        var shipDate = IsBusinessDay(orderDate) && orderTime < CutoffTime
            ? orderDate
            : NextBusinessDay(orderDate);

        if (shipNotBefore is { } earliest && earliest > shipDate)
        {
            shipDate = IsBusinessDay(earliest) ? earliest : NextBusinessDay(earliest);
        }

        var deliveryDate = shipDate;
        for (var i = 0; i < transitDays; i++)
        {
            deliveryDate = NextBusinessDay(deliveryDate);
        }

        return new DeliveryEstimate(shipDate, deliveryDate, transitDays);
    }

    private DateOnly NextBusinessDay(DateOnly date)
    {
        var next = date.AddDays(1);
        while (!IsBusinessDay(next))
        {
            next = next.AddDays(1);
        }

        return next;
    }
}
