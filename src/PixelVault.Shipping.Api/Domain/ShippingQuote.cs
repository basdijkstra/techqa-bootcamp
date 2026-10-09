namespace PixelVault.Shipping.Domain;

/// <summary>
/// A shipping quote for one address and basket. Valid for 30 minutes; the Order service checks it at checkout.
/// </summary>
public class ShippingQuote
{
    public Guid Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public string Line1 { get; set; } = "";

    public string? Line2 { get; set; }

    public string City { get; set; } = "";

    public string Region { get; set; } = "";

    public string PostalCode { get; set; } = "";

    public string Country { get; set; } = "";

    public ShippingZone Zone { get; set; }

    public int ItemCount { get; set; }

    public decimal OrderSubtotal { get; set; }

    public DateOnly? ShipNotBefore { get; set; }

    public List<ShippingQuoteOption> Options { get; set; } = [];
}

public class ShippingQuoteOption
{
    public int Id { get; set; }

    public Guid QuoteId { get; set; }

    public ShippingMethod Method { get; set; }

    public decimal Cost { get; set; }

    public bool IsFree { get; set; }

    public DateOnly ShipDate { get; set; }

    public DateOnly EstimatedDeliveryDate { get; set; }

    public int TransitBusinessDays { get; set; }
}
