using PixelVault.Shipping.Domain;

namespace PixelVault.Shipping.Dtos;

public record AddressDto(string? Line1, string? Line2, string? City, string? Region, string? PostalCode, string? Country);

public record AddressValidationResultDto(bool IsValid, IDictionary<string, string[]> Errors, AddressDto? NormalizedAddress);

public record CreateQuoteRequest(AddressDto? Address, int ItemCount, decimal OrderSubtotal, DateOnly? ShipNotBefore);

public record ShippingOptionDto(
    ShippingMethod Method,
    decimal Cost,
    bool IsFree,
    DateOnly ShipDate,
    DateOnly EstimatedDeliveryDate,
    int TransitBusinessDays);

public record ShippingQuoteDto(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    bool IsExpired,
    AddressDto Address,
    ShippingZone Zone,
    int ItemCount,
    decimal OrderSubtotal,
    DateOnly? ShipNotBefore,
    IReadOnlyList<ShippingOptionDto> Options,
    IReadOnlyList<string> Notices);

public record HolidayDto(DateOnly Date, DateOnly ObservedOn, string Name);
