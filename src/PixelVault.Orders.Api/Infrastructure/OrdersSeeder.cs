using PixelVault.Orders.Domain;
using PixelVault.Shared.Seed;
using PixelVault.Shared.TestSupport;

namespace PixelVault.Orders.Infrastructure;

/// <summary>Seeds the 15 historical orders from <see cref="SeedOrders"/>, with a status history for each.</summary>
public class OrdersSeeder(OrdersDbContext db, TimeProvider timeProvider) : IDataSeeder
{
    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Database.EnsureCreatedAsync(cancellationToken))
        {
            await SeedAsync(cancellationToken);
        }
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureDeletedAsync(cancellationToken);
        await db.Database.EnsureCreatedAsync(cancellationToken);
        await SeedAsync(cancellationToken);
    }

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        foreach (var seed in SeedOrders.All)
        {
            db.Orders.Add(CreateOrder(seed, now));
        }

        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    private static Order CreateOrder(SeedOrder seed, DateTimeOffset now)
    {
        var createdAt = seed.CreatedAt(now);
        var status = Enum.Parse<OrderStatus>(seed.Status);
        DateTimeOffset? At(double? daysAgo) => daysAgo is { } d ? now.AddDays(-d) : null;

        var paid = status != OrderStatus.PaymentFailed;
        var order = new Order
        {
            OrderNumber = seed.OrderNumber(now),
            BasketId = Guid.NewGuid(),
            Status = status,
            CustomerName = seed.Customer.FullName,
            CustomerEmail = seed.Customer.Email,
            CustomerPhone = seed.Customer.Phone,
            ShippingLine1 = seed.Address.Line1,
            ShippingCity = seed.Address.City,
            ShippingRegion = seed.Address.Region,
            ShippingPostalCode = seed.Address.PostalCode,
            ShippingCountry = seed.Address.Country,
            ShippingMethod = seed.ShippingMethod,
            ShippingCost = seed.ShippingCost,
            EstimatedShipDate = DateOnly.FromDateTime(createdAt.UtcDateTime.AddDays(1)),
            EstimatedDeliveryDate = DateOnly.FromDateTime(createdAt.UtcDateTime.AddDays(seed.ShippingMethod == "Express" ? 3 : 7)),
            Subtotal = seed.Subtotal,
            DiscountTotal = seed.Discount,
            DiscountDescription = seed.DiscountDescription,
            TaxRate = seed.TaxRate,
            TaxAmount = seed.Tax,
            Total = seed.Total,
            PaymentId = seed.PaymentId,
            CardBrand = seed.CardBrand,
            CardLast4 = seed.CardLast4,
            PaymentFailureCode = seed.DeclineCode,
            TrackingNumber = seed.TrackingNumber,
            CreatedAt = createdAt,
            PaidAt = paid ? createdAt.AddSeconds(3) : null,
            ShippedAt = At(seed.ShippedDaysAgo),
            DeliveredAt = At(seed.DeliveredDaysAgo),
            CancelledAt = status == OrderStatus.Cancelled ? At(seed.ClosedDaysAgo) : null,
            RefundedAt = status == OrderStatus.Refunded ? At(seed.ClosedDaysAgo) : null,
            CancellationReason = status == OrderStatus.Cancelled ? "Customer changed their mind" : null,
            Lines = seed.Lines.Select(l => new OrderLine
            {
                GameId = l.GameId,
                Title = l.Title,
                UnitPrice = l.UnitPrice,
                Quantity = l.Quantity,
                LineTotal = l.LineTotal,
                IsPreOrder = l.IsPreOrder,
                ReleaseDate = l.IsPreOrder ? DateOnly.FromDateTime(now.UtcDateTime.AddDays(45)) : null
            }).ToList()
        };

        void History(OrderStatus? from, OrderStatus to, DateTimeOffset? at, string by, string? note)
        {
            if (at is { } changedAt)
            {
                order.History.Add(new OrderStatusChange { FromStatus = from, ToStatus = to, ChangedAt = changedAt, ChangedBy = by, Note = note });
            }
        }

        History(null, OrderStatus.Pending, createdAt, "customer", "Order placed");
        if (!paid)
        {
            History(OrderStatus.Pending, OrderStatus.PaymentFailed, createdAt.AddSeconds(3), "system", $"Payment failed: {seed.DeclineCode}");
            return order;
        }

        History(OrderStatus.Pending, OrderStatus.Paid, order.PaidAt, "system", "Payment approved");
        History(OrderStatus.Paid, OrderStatus.Shipped, order.ShippedAt, "admin", null);
        History(OrderStatus.Shipped, OrderStatus.Delivered, order.DeliveredAt, "admin", null);
        History(OrderStatus.Paid, OrderStatus.Cancelled, order.CancelledAt, "customer", order.CancellationReason);
        History(OrderStatus.Delivered, OrderStatus.Refunded, order.RefundedAt, "admin", "Customer return");
        return order;
    }
}
