using PixelVault.Basket.Domain;

namespace PixelVault.Basket.Dtos;

public enum AppliedDiscount
{
    None,
    Bundle,
    PromoCode
}

public record BasketLineDto(
    int GameId,
    string Slug,
    string Title,
    string Category,
    string AgeRating,
    int MinimumAge,
    bool IsPreOrder,
    DateOnly? ReleaseDate,
    int Quantity,
    decimal UnitPrice,
    decimal UnitPriceWhenAdded,
    bool PriceChanged,
    decimal LineTotal,
    string StockStatus,
    int AvailableQuantity,
    string? CoverColorPrimary,
    string? CoverColorSecondary,
    IReadOnlyList<string> Issues);

public record PromoCodeStatusDto(
    string Code,
    string? Description,
    bool IsValid,
    bool IsApplied,
    decimal Discount,
    string? FailureReason,
    string Message);

public record BasketDto(
    Guid Id,
    BasketStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastActivityAt,
    DateTimeOffset ExpiresAt,
    string? OrderReference,
    IReadOnlyList<BasketLineDto> Items,
    int ItemCount,
    decimal Subtotal,
    decimal BundleDiscount,
    PromoCodeStatusDto? PromoCode,
    AppliedDiscount AppliedDiscount,
    decimal DiscountTotal,
    string? DiscountDescription,
    decimal Total,
    int MinimumAge,
    DateOnly? LatestReleaseDate,
    bool HasIssues,
    bool CanCheckout,
    IReadOnlyList<string> Messages);

public record AddItemRequest(int GameId, int Quantity = 1);

public record UpdateItemRequest(int Quantity);

public record ApplyPromoCodeRequest(string? Code);

public record CheckoutBasketRequest(string? OrderReference);
