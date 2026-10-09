using PixelVault.Orders.Domain;
using PixelVault.Orders.Dtos;
using PixelVault.Orders.Repositories;
using PixelVault.Shared.Errors;

namespace PixelVault.Orders.Services;

public interface IOrderService
{
    /// <summary>Looks up an order for a customer. The email address must match, otherwise it is "not found".</summary>
    Task<OrderDto> GetForCustomerAsync(string orderNumber, string? email, CancellationToken cancellationToken = default);

    Task<OrderDto> CancelByCustomerAsync(string orderNumber, CancelOrderRequest request, CancellationToken cancellationToken = default);
}

public class OrderService(
    IOrderRepository orders,
    IOrderStateMachine stateMachine,
    ICancellationPolicy cancellationPolicy,
    IOrderCompensation compensation,
    IOrderMapper mapper,
    TimeProvider timeProvider) : IOrderService
{
    public async Task<OrderDto> GetForCustomerAsync(string orderNumber, string? email, CancellationToken cancellationToken = default)
    {
        var order = await GetMatchingOrderAsync(orderNumber, email, cancellationToken);
        return mapper.ToDto(order, timeProvider.GetUtcNow());
    }

    public async Task<OrderDto> CancelByCustomerAsync(string orderNumber, CancelOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await GetMatchingOrderAsync(orderNumber, request.Email, cancellationToken);
        var now = timeProvider.GetUtcNow();

        if (order.Status != OrderStatus.Paid)
        {
            throw new ConflictException("ORDER_NOT_CANCELLABLE",
                order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered
                    ? "This order has already shipped and can no longer be cancelled."
                    : $"An order with status {order.Status} cannot be cancelled.");
        }

        if (!cancellationPolicy.CanCustomerCancel(order, now))
        {
            throw new BusinessRuleException("CANCELLATION_WINDOW_EXPIRED",
                "Orders can only be cancelled within 24 hours. Please contact customer support.");
        }

        stateMachine.EnsureCanTransition(order.Status, OrderStatus.Cancelled);
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? "Cancelled by customer" : request.Reason.Trim();
        if (reason.Length > 200)
        {
            throw new ValidationException("reason", "Reason cannot be longer than 200 characters.");
        }

        await compensation.RefundPaymentAsync(order, reason, cancellationToken);
        await compensation.ReleaseStockAsync(order, cancellationToken);

        order.History.Add(new OrderStatusChange
        {
            FromStatus = order.Status, ToStatus = OrderStatus.Cancelled, ChangedAt = now, ChangedBy = "customer", Note = reason
        });
        order.Status = OrderStatus.Cancelled;
        order.CancelledAt = now;
        order.CancellationReason = reason;
        await orders.SaveChangesAsync(cancellationToken);

        return mapper.ToDto(order, now);
    }

    private async Task<Order> GetMatchingOrderAsync(string orderNumber, string? email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("email", "Email address is required to look up an order.");
        }

        var order = await orders.GetByOrderNumberAsync(orderNumber.Trim().ToUpperInvariant(), cancellationToken);
        if (order is null || !string.Equals(order.CustomerEmail, email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new NotFoundException("Order", orderNumber);
        }

        return order;
    }
}
