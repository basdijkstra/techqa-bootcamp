namespace PixelVault.Orders.Domain;

public enum OrderStatus
{
    /// <summary>Stock is reserved and the payment is being processed.</summary>
    Pending,

    /// <summary>The payment was declined or failed. Nothing will be shipped.</summary>
    PaymentFailed,

    Paid,

    Shipped,

    Delivered,

    /// <summary>Cancelled before shipping. The payment was refunded and the stock released.</summary>
    Cancelled,

    /// <summary>Refunded after delivery.</summary>
    Refunded
}

public class Order
{
    public int Id { get; set; }

    /// <summary>For example PV-20261009-K7M2Q.</summary>
    public string OrderNumber { get; set; } = "";

    public Guid BasketId { get; set; }

    public OrderStatus Status { get; set; }

    public string CustomerName { get; set; } = "";

    /// <summary>Stored in lower case.</summary>
    public string CustomerEmail { get; set; } = "";

    /// <summary>10 digits, e.g. 2125550123.</summary>
    public string CustomerPhone { get; set; } = "";

    public string ShippingLine1 { get; set; } = "";

    public string? ShippingLine2 { get; set; }

    public string ShippingCity { get; set; } = "";

    public string ShippingRegion { get; set; } = "";

    public string ShippingPostalCode { get; set; } = "";

    public string ShippingCountry { get; set; } = "";

    public string ShippingMethod { get; set; } = "";

    public decimal ShippingCost { get; set; }

    public DateOnly? EstimatedShipDate { get; set; }

    public DateOnly? EstimatedDeliveryDate { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DiscountTotal { get; set; }

    public string? DiscountDescription { get; set; }

    public decimal TaxRate { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal Total { get; set; }

    public Guid? PaymentId { get; set; }

    public string? CardBrand { get; set; }

    public string? CardLast4 { get; set; }

    public string? PaymentFailureCode { get; set; }

    public string? TrackingNumber { get; set; }

    public string? CancellationReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? PaidAt { get; set; }

    public DateTimeOffset? ShippedAt { get; set; }

    public DateTimeOffset? DeliveredAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public DateTimeOffset? RefundedAt { get; set; }

    public List<OrderLine> Lines { get; set; } = [];

    public List<OrderStatusChange> History { get; set; } = [];
}

public class OrderLine
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int GameId { get; set; }

    public string Title { get; set; } = "";

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }

    public bool IsPreOrder { get; set; }

    public DateOnly? ReleaseDate { get; set; }
}

public class OrderStatusChange
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public OrderStatus? FromStatus { get; set; }

    public OrderStatus ToStatus { get; set; }

    public DateTimeOffset ChangedAt { get; set; }

    /// <summary>"customer", "system" or a staff username.</summary>
    public string ChangedBy { get; set; } = "";

    public string? Note { get; set; }
}
