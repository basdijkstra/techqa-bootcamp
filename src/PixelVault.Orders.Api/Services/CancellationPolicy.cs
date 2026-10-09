using PixelVault.Orders.Domain;

namespace PixelVault.Orders.Services;

public interface ICancellationPolicy
{
    /// <summary>Until when the customer can cancel the order themselves, or null if they never can.</summary>
    DateTimeOffset? CustomerCancellationDeadline(Order order);

    bool CanCustomerCancel(Order order, DateTimeOffset now);

    /// <summary>Until when staff can refund a delivered order, or null if it was not delivered.</summary>
    DateTimeOffset? RefundDeadline(Order order);

    bool CanRefund(Order order, DateTimeOffset now);
}

/// <summary>
/// <list type="bullet">
/// <item>Customers can cancel a paid order that has not shipped yet, up to 24 hours after payment.</item>
/// <item>Staff can cancel any paid order that has not shipped yet.</item>
/// <item>Delivered orders can be refunded up to 30 days after delivery.</item>
/// </list>
/// </summary>
public class CancellationPolicy : ICancellationPolicy
{
    public static readonly TimeSpan CustomerCancellationWindow = TimeSpan.FromHours(24);
    public static readonly TimeSpan RefundWindow = TimeSpan.FromDays(30);

    public DateTimeOffset? CustomerCancellationDeadline(Order order) =>
        order.PaidAt?.Add(CustomerCancellationWindow);

    public bool CanCustomerCancel(Order order, DateTimeOffset now) =>
        order.Status == OrderStatus.Paid && CustomerCancellationDeadline(order) is { } deadline && now < deadline;

    public DateTimeOffset? RefundDeadline(Order order) =>
        order.DeliveredAt?.Add(RefundWindow);

    public bool CanRefund(Order order, DateTimeOffset now) =>
        order.Status == OrderStatus.Delivered && RefundDeadline(order) is { } deadline && now <= deadline;
}
