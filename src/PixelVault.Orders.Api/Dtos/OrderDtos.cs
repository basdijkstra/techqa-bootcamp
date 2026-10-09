using PixelVault.Orders.Domain;

namespace PixelVault.Orders.Dtos;

public record CustomerDetailsDto(string? FullName, string? Email, string? Phone);

public record PaymentCardDto(string? Number, string? CardholderName, int ExpiryMonth, int ExpiryYear, string? Cvv);

public record AgeVerificationDto(DateOnly? DateOfBirth);

public record PlaceOrderRequest(
    Guid BasketId,
    CustomerDetailsDto? Customer,
    Guid ShippingQuoteId,
    string? ShippingMethod,
    PaymentCardDto? Payment,
    AgeVerificationDto? AgeVerification);

public record PreviewOrderRequest(Guid BasketId, Guid ShippingQuoteId, string? ShippingMethod);

public record OrderPreviewDto(
    decimal Subtotal,
    decimal DiscountTotal,
    string? DiscountDescription,
    string ShippingMethod,
    decimal ShippingCost,
    decimal TaxRate,
    decimal TaxAmount,
    decimal Total,
    DateOnly EstimatedShipDate,
    DateOnly EstimatedDeliveryDate,
    int MinimumAge,
    bool RequiresAgeVerification);

public record AddressDto(string Line1, string? Line2, string City, string Region, string PostalCode, string Country);

public record OrderLineDto(int GameId, string Title, decimal UnitPrice, int Quantity, decimal LineTotal, bool IsPreOrder, DateOnly? ReleaseDate);

public record OrderStatusChangeDto(OrderStatus? FromStatus, OrderStatus ToStatus, DateTimeOffset ChangedAt, string ChangedBy, string? Note);

public record OrderDto(
    string OrderNumber,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    string CustomerName,
    string CustomerEmail,
    string CustomerPhone,
    AddressDto ShippingAddress,
    string ShippingMethod,
    DateOnly? EstimatedShipDate,
    DateOnly? EstimatedDeliveryDate,
    IReadOnlyList<OrderLineDto> Lines,
    decimal Subtotal,
    decimal DiscountTotal,
    string? DiscountDescription,
    decimal ShippingCost,
    decimal TaxRate,
    decimal TaxAmount,
    decimal Total,
    string? CardBrand,
    string? CardLast4,
    string? TrackingNumber,
    DateTimeOffset? PaidAt,
    DateTimeOffset? ShippedAt,
    DateTimeOffset? DeliveredAt,
    DateTimeOffset? CancelledAt,
    DateTimeOffset? RefundedAt,
    string? CancellationReason,
    bool CanBeCancelledByCustomer,
    DateTimeOffset? CancellationDeadline);

public record AdminOrderDto(
    OrderDto Order,
    Guid BasketId,
    Guid? PaymentId,
    string? PaymentFailureCode,
    IReadOnlyList<OrderStatus> AllowedStatusChanges,
    IReadOnlyList<OrderStatusChangeDto> History);

public record OrderListItemDto(
    string OrderNumber,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    string CustomerName,
    string CustomerEmail,
    string ShippingCountry,
    string ShippingRegion,
    int ItemCount,
    decimal Total);

public record OrderListQuery(
    string? Status = null,
    DateOnly? From = null,
    DateOnly? To = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public record OrderSummaryDto(
    int TotalOrders,
    IReadOnlyDictionary<string, int> CountByStatus,
    int OrdersToday,
    int AwaitingShipment,
    decimal RevenueLast7Days,
    decimal RevenueAllTime);

public record ChangeOrderStatusRequest(string? Status, string? TrackingNumber, string? Note);

public record CancelOrderRequest(string? Email, string? Reason);
