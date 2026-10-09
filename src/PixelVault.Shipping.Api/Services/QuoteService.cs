using PixelVault.Shipping.Domain;
using PixelVault.Shipping.Dtos;
using PixelVault.Shipping.Repositories;
using PixelVault.Shared.Errors;

namespace PixelVault.Shipping.Services;

public interface IQuoteService
{
    Task<ShippingQuoteDto> CreateQuoteAsync(CreateQuoteRequest request, CancellationToken cancellationToken = default);

    Task<ShippingQuoteDto> GetQuoteAsync(Guid quoteId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Creates shipping quotes. A quote lists every shipping method available for the address, with its cost and
/// estimated delivery date. Quotes are valid for 30 minutes. A basket holds 1 to 10 items.
/// </summary>
public class QuoteService(
    IQuoteRepository quotes,
    IAddressValidator addressValidator,
    IShippingZoneResolver zoneResolver,
    IShippingCostCalculator costCalculator,
    IDeliveryDateEstimator deliveryEstimator,
    TimeProvider timeProvider) : IQuoteService
{
    public static readonly TimeSpan QuoteLifetime = TimeSpan.FromMinutes(30);
    public const int MaxItems = 10;

    public async Task<ShippingQuoteDto> CreateQuoteAsync(CreateQuoteRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new ValidationErrors();
        errors.AddIf(request.ItemCount is < 1 or > MaxItems, "itemCount", $"Item count must be between 1 and {MaxItems}.");
        errors.AddIf(request.OrderSubtotal < 0, "orderSubtotal", "Order subtotal cannot be negative.");

        var validation = addressValidator.Validate(request.Address);
        foreach (var (field, messages) in validation.Errors)
        {
            foreach (var message in messages)
            {
                errors.Add(field == "address" ? field : $"address.{field}", message);
            }
        }

        errors.ThrowIfAny();

        var address = validation.NormalizedAddress!;
        var zone = zoneResolver.Resolve(address.Country!, address.Region!);
        var now = timeProvider.GetUtcNow();

        var quote = new ShippingQuote
        {
            Id = Guid.NewGuid(),
            CreatedAt = now,
            ExpiresAt = now.Add(QuoteLifetime),
            Line1 = address.Line1!,
            Line2 = address.Line2,
            City = address.City!,
            Region = address.Region!,
            PostalCode = address.PostalCode!,
            Country = address.Country!,
            Zone = zone,
            ItemCount = request.ItemCount,
            OrderSubtotal = request.OrderSubtotal,
            ShipNotBefore = request.ShipNotBefore
        };

        foreach (var method in costCalculator.AvailableMethods(zone))
        {
            var cost = costCalculator.Calculate(zone, method, request.ItemCount, request.OrderSubtotal);
            var estimate = deliveryEstimator.Estimate(zone, method, now, request.ShipNotBefore);
            quote.Options.Add(new ShippingQuoteOption
            {
                QuoteId = quote.Id,
                Method = method,
                Cost = cost.Cost,
                IsFree = cost.IsFree,
                ShipDate = estimate.ShipDate,
                EstimatedDeliveryDate = estimate.DeliveryDate,
                TransitBusinessDays = estimate.TransitBusinessDays
            });
        }

        quotes.Add(quote);
        await quotes.SaveChangesAsync(cancellationToken);
        return ToDto(quote, now);
    }

    public async Task<ShippingQuoteDto> GetQuoteAsync(Guid quoteId, CancellationToken cancellationToken = default)
    {
        var quote = await quotes.GetAsync(quoteId, cancellationToken) ?? throw new NotFoundException("Quote", quoteId);
        return ToDto(quote, timeProvider.GetUtcNow());
    }

    private static ShippingQuoteDto ToDto(ShippingQuote quote, DateTimeOffset now)
    {
        var notices = new List<string>();
        if (quote.Zone == ShippingZone.Canada)
        {
            notices.Add("Orders to Canada may be charged import duties and taxes on delivery. These are paid by the customer.");
            notices.Add("Express shipping is not available to Canada.");
        }

        if (quote.ShipNotBefore is { } shipNotBefore)
        {
            notices.Add($"Your order contains a pre-order and will ship when it is released on {shipNotBefore:MMMM d, yyyy}.");
        }

        var standard = quote.Options.FirstOrDefault(o => o.Method == ShippingMethod.Standard);
        if (quote.Zone == ShippingZone.ContiguousUS && standard is { IsFree: false })
        {
            notices.Add($"Spend {ShippingCostCalculator.FreeShippingThreshold - quote.OrderSubtotal:C} more for free standard shipping.");
        }

        return new ShippingQuoteDto(
            quote.Id,
            quote.CreatedAt,
            quote.ExpiresAt,
            now >= quote.ExpiresAt,
            new AddressDto(quote.Line1, quote.Line2, quote.City, quote.Region, quote.PostalCode, quote.Country),
            quote.Zone,
            quote.ItemCount,
            quote.OrderSubtotal,
            quote.ShipNotBefore,
            quote.Options
                .OrderBy(o => o.Method)
                .Select(o => new ShippingOptionDto(o.Method, o.Cost, o.IsFree, o.ShipDate, o.EstimatedDeliveryDate, o.TransitBusinessDays))
                .ToList(),
            notices);
    }
}
