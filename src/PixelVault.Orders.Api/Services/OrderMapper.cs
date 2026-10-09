using PixelVault.Orders.Domain;
using PixelVault.Orders.Dtos;

namespace PixelVault.Orders.Services;

public interface IOrderMapper
{
    OrderDto ToDto(Order order, DateTimeOffset now);

    AdminOrderDto ToAdminDto(Order order, DateTimeOffset now);

    OrderListItemDto ToListItem(Order order);
}

public class OrderMapper(ICancellationPolicy cancellationPolicy, IOrderStateMachine stateMachine) : IOrderMapper
{
    public OrderDto ToDto(Order order, DateTimeOffset now)
    {
        var canCancel = cancellationPolicy.CanCustomerCancel(order, now);
        return new OrderDto(
            order.OrderNumber,
            order.Status,
            order.CreatedAt,
            order.CustomerName,
            order.CustomerEmail,
            order.CustomerPhone,
            new AddressDto(order.ShippingLine1, order.ShippingLine2, order.ShippingCity, order.ShippingRegion,
                order.ShippingPostalCode, order.ShippingCountry),
            order.ShippingMethod,
            order.EstimatedShipDate,
            order.EstimatedDeliveryDate,
            order.Lines
                .OrderBy(l => l.Id)
                .Select(l => new OrderLineDto(l.GameId, l.Title, l.UnitPrice, l.Quantity, l.LineTotal, l.IsPreOrder, l.ReleaseDate))
                .ToList(),
            order.Subtotal,
            order.DiscountTotal,
            order.DiscountDescription,
            order.ShippingCost,
            order.TaxRate,
            order.TaxAmount,
            order.Total,
            order.CardBrand,
            order.CardLast4,
            order.TrackingNumber,
            order.PaidAt,
            order.ShippedAt,
            order.DeliveredAt,
            order.CancelledAt,
            order.RefundedAt,
            order.CancellationReason,
            canCancel,
            canCancel ? cancellationPolicy.CustomerCancellationDeadline(order) : null);
    }

    public AdminOrderDto ToAdminDto(Order order, DateTimeOffset now) => new(
        ToDto(order, now),
        order.BasketId,
        order.PaymentId,
        order.PaymentFailureCode,
        stateMachine.AllowedStaffTransitions(order.Status),
        order.History
            .OrderBy(h => h.ChangedAt)
            .ThenBy(h => h.Id)
            .Select(h => new OrderStatusChangeDto(h.FromStatus, h.ToStatus, h.ChangedAt, h.ChangedBy, h.Note))
            .ToList());

    public OrderListItemDto ToListItem(Order order) => new(
        order.OrderNumber,
        order.Status,
        order.CreatedAt,
        order.CustomerName,
        order.CustomerEmail,
        order.ShippingCountry,
        order.ShippingRegion,
        order.Lines.Sum(l => l.Quantity),
        order.Total);
}
