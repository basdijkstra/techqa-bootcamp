using PixelVault.Orders.Clients;
using PixelVault.Orders.Domain;

namespace PixelVault.Orders.Services;

public interface IOrderCompensation
{
    /// <summary>Refunds the full payment of an order, if it has one.</summary>
    Task RefundPaymentAsync(Order order, string reason, CancellationToken cancellationToken = default);

    /// <summary>Puts the order's stock back in the Catalog.</summary>
    Task ReleaseStockAsync(Order order, CancellationToken cancellationToken = default);
}

public class OrderCompensation(IPaymentClient payments, ICatalogClient catalog, ILogger<OrderCompensation> logger) : IOrderCompensation
{
    public async Task RefundPaymentAsync(Order order, string reason, CancellationToken cancellationToken = default)
    {
        if (order.PaymentId is not { } paymentId)
        {
            logger.LogWarning("Order {OrderNumber} has no payment to refund", order.OrderNumber);
            return;
        }

        await payments.RefundAsync(paymentId, order.Total, reason, cancellationToken);
    }

    public async Task ReleaseStockAsync(Order order, CancellationToken cancellationToken = default)
    {
        try
        {
            await catalog.ReleaseReservationAsync(order.OrderNumber, cancellationToken);
        }
        catch (Exception ex)
        {
            // The order is still cancelled. Stock can be corrected by hand in the admin pages.
            logger.LogError(ex, "Could not release stock for order {OrderNumber}", order.OrderNumber);
        }
    }
}
