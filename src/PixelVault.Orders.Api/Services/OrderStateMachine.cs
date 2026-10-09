using PixelVault.Orders.Domain;
using PixelVault.Shared.Errors;

namespace PixelVault.Orders.Services;

public interface IOrderStateMachine
{
    bool CanTransition(OrderStatus from, OrderStatus to);

    /// <summary>Throws a 409 INVALID_STATUS_TRANSITION when the change is not allowed.</summary>
    void EnsureCanTransition(OrderStatus from, OrderStatus to);

    IReadOnlyList<OrderStatus> AllowedTransitions(OrderStatus from);

    /// <summary>The statuses staff can move an order to from its current status.</summary>
    IReadOnlyList<OrderStatus> AllowedStaffTransitions(OrderStatus from);
}

/// <summary>
/// Order lifecycle:
/// <code>
/// Pending ──► Paid ──► Shipped ──► Delivered ──► Refunded
///    │          │
///    │          └────► Cancelled
///    ├───────────────► Cancelled
///    └───────────────► PaymentFailed
/// </code>
/// PaymentFailed, Cancelled and Refunded are final. Pending and PaymentFailed are set by the checkout only;
/// staff can ship, deliver, cancel and refund.
/// </summary>
public class OrderStateMachine : IOrderStateMachine
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Transitions = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Paid, OrderStatus.PaymentFailed, OrderStatus.Cancelled],
        [OrderStatus.Paid] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.Delivered],
        [OrderStatus.Delivered] = [OrderStatus.Refunded],
        [OrderStatus.PaymentFailed] = [],
        [OrderStatus.Cancelled] = [],
        [OrderStatus.Refunded] = []
    };

    private static readonly OrderStatus[] StaffTargets =
        [OrderStatus.Shipped, OrderStatus.Delivered, OrderStatus.Cancelled, OrderStatus.Refunded];

    public bool CanTransition(OrderStatus from, OrderStatus to) =>
        Transitions.TryGetValue(from, out var targets) && targets.Contains(to);

    public void EnsureCanTransition(OrderStatus from, OrderStatus to)
    {
        if (!CanTransition(from, to))
        {
            var exception = new ConflictException("INVALID_STATUS_TRANSITION", $"An order cannot go from {from} to {to}.");
            exception.Details["currentStatus"] = from.ToString();
            exception.Details["allowedStatuses"] = AllowedTransitions(from).Select(s => s.ToString()).ToArray();
            throw exception;
        }
    }

    public IReadOnlyList<OrderStatus> AllowedTransitions(OrderStatus from) =>
        Transitions.TryGetValue(from, out var targets) ? targets : [];

    public IReadOnlyList<OrderStatus> AllowedStaffTransitions(OrderStatus from) =>
        AllowedTransitions(from).Where(StaffTargets.Contains).ToList();
}
