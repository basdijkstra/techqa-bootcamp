using System.Text.RegularExpressions;
using PixelVault.Orders.Domain;
using PixelVault.Orders.Dtos;
using PixelVault.Orders.Repositories;
using PixelVault.Shared.Errors;

namespace PixelVault.Orders.Services;

public interface IAdminOrderService
{
    Task<PagedResult<OrderListItemDto>> ListAsync(OrderListQuery query, CancellationToken cancellationToken = default);

    Task<AdminOrderDto> GetAsync(string orderNumber, CancellationToken cancellationToken = default);

    Task<OrderSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<AdminOrderDto> ChangeStatusAsync(string orderNumber, ChangeOrderStatusRequest request, string changedBy, CancellationToken cancellationToken = default);
}

/// <summary>
/// Staff actions on orders:
/// <list type="bullet">
/// <item>Shipped: needs a tracking number (10 to 30 letters and digits). Orders with a pre-order cannot ship
/// before the release date.</item>
/// <item>Delivered: no extra input.</item>
/// <item>Cancelled: needs a note. Refunds the payment and puts the stock back.</item>
/// <item>Refunded: needs a note, and is only possible up to 30 days after delivery. Refunds the payment.</item>
/// </list>
/// </summary>
public partial class AdminOrderService(
    IOrderRepository orders,
    IOrderStateMachine stateMachine,
    ICancellationPolicy cancellationPolicy,
    IOrderCompensation compensation,
    IOrderMapper mapper,
    TimeProvider timeProvider) : IAdminOrderService
{
    public const int MaxPageSize = 100;

    [GeneratedRegex("^[A-Z0-9]{10,30}$")]
    private static partial Regex TrackingNumberPattern();

    public async Task<PagedResult<OrderListItemDto>> ListAsync(OrderListQuery query, CancellationToken cancellationToken = default)
    {
        var errors = new ValidationErrors();
        OrderStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (Enum.TryParse<OrderStatus>(query.Status, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
            {
                status = parsed;
            }
            else
            {
                errors.Add("status", $"Unknown status '{query.Status}'.");
            }
        }

        errors.AddIf(query.From > query.To, "from", "'From' cannot be after 'to'.");
        errors.AddIf(query.Page < 1, "page", "Page must be 1 or higher.");
        errors.AddIf(query.PageSize is < 1 or > MaxPageSize, "pageSize", $"Page size must be between 1 and {MaxPageSize}.");
        errors.AddIf(query.Search?.Length > 100, "search", "Search text cannot be longer than 100 characters.");
        errors.ThrowIfAny();

        var filter = new OrderFilter(
            status,
            query.From is { } from ? new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null,
            query.To is { } to ? new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null,
            string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim());

        var (items, total) = await orders.FindAsync(filter, (query.Page - 1) * query.PageSize, query.PageSize, cancellationToken);
        return new PagedResult<OrderListItemDto>(
            items.Select(mapper.ToListItem).ToList(),
            query.Page,
            query.PageSize,
            total,
            (int)Math.Ceiling(total / (double)query.PageSize));
    }

    public async Task<AdminOrderDto> GetAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        var order = await LoadAsync(orderNumber, cancellationToken);
        return mapper.ToAdminDto(order, timeProvider.GetUtcNow());
    }

    public async Task<OrderSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var all = await orders.GetAllAsync(cancellationToken);
        var revenueStatuses = new[] { OrderStatus.Paid, OrderStatus.Shipped, OrderStatus.Delivered };
        var revenueOrders = all.Where(o => revenueStatuses.Contains(o.Status)).ToList();

        return new OrderSummaryDto(
            all.Count,
            Enum.GetValues<OrderStatus>().ToDictionary(s => s.ToString(), s => all.Count(o => o.Status == s)),
            all.Count(o => o.CreatedAt.UtcDateTime.Date == now.UtcDateTime.Date),
            all.Count(o => o.Status == OrderStatus.Paid),
            revenueOrders.Where(o => o.CreatedAt >= now.AddDays(-7)).Sum(o => o.Total),
            revenueOrders.Sum(o => o.Total));
    }

    public async Task<AdminOrderDto> ChangeStatusAsync(string orderNumber, ChangeOrderStatusRequest request, string changedBy, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, ignoreCase: true, out var target) || !Enum.IsDefined(target))
        {
            throw new ValidationException("status", "Status must be Shipped, Delivered, Cancelled or Refunded.");
        }

        var order = await LoadAsync(orderNumber, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        if (!stateMachine.AllowedStaffTransitions(order.Status).Contains(target))
        {
            stateMachine.EnsureCanTransition(order.Status, target);
            throw new ConflictException("INVALID_STATUS_TRANSITION", $"Staff cannot change an order from {order.Status} to {target}.");
        }

        if (note?.Length > 200)
        {
            throw new ValidationException("note", "Note cannot be longer than 200 characters.");
        }

        switch (target)
        {
            case OrderStatus.Shipped:
                MarkShipped(order, request.TrackingNumber, now);
                break;
            case OrderStatus.Delivered:
                order.DeliveredAt = now;
                break;
            case OrderStatus.Cancelled:
                if (note is null)
                {
                    throw new ValidationException("note", "A note is required when cancelling an order.");
                }

                await compensation.RefundPaymentAsync(order, note, cancellationToken);
                await compensation.ReleaseStockAsync(order, cancellationToken);
                order.CancelledAt = now;
                order.CancellationReason = note;
                break;
            case OrderStatus.Refunded:
                if (note is null)
                {
                    throw new ValidationException("note", "A note is required when refunding an order.");
                }

                if (!cancellationPolicy.CanRefund(order, now))
                {
                    throw new BusinessRuleException("REFUND_WINDOW_EXPIRED", "Orders can only be refunded up to 30 days after delivery.");
                }

                await compensation.RefundPaymentAsync(order, note, cancellationToken);
                order.RefundedAt = now;
                break;
        }

        order.History.Add(new OrderStatusChange { FromStatus = order.Status, ToStatus = target, ChangedAt = now, ChangedBy = changedBy, Note = note });
        order.Status = target;
        await orders.SaveChangesAsync(cancellationToken);

        return mapper.ToAdminDto(order, now);
    }

    private static void MarkShipped(Order order, string? trackingNumber, DateTimeOffset now)
    {
        var tracking = (trackingNumber ?? "").Replace(" ", "").ToUpperInvariant();
        if (!TrackingNumberPattern().IsMatch(tracking))
        {
            throw new ValidationException("trackingNumber", "Tracking number must be 10 to 30 letters and digits.");
        }

        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var unreleased = order.Lines.Where(l => l.IsPreOrder && l.ReleaseDate > today).ToList();
        if (unreleased.Count > 0)
        {
            var releaseDate = unreleased.Max(l => l.ReleaseDate)!.Value;
            throw new BusinessRuleException("PREORDER_NOT_RELEASED",
                $"This order contains a pre-order that is released on {releaseDate:MMMM d, yyyy}. It cannot ship before then.");
        }

        order.TrackingNumber = tracking;
        order.ShippedAt = now;
    }

    private async Task<Order> LoadAsync(string orderNumber, CancellationToken cancellationToken) =>
        await orders.GetByOrderNumberAsync(orderNumber.Trim().ToUpperInvariant(), cancellationToken)
        ?? throw new NotFoundException("Order", orderNumber);
}
