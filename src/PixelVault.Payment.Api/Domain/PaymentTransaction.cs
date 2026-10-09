namespace PixelVault.Payment.Domain;

public enum PaymentStatus
{
    /// <summary>The money was taken.</summary>
    Captured,

    /// <summary>The card issuer or the fraud check refused the payment.</summary>
    Declined,

    /// <summary>The payment processor had an error. No money was taken.</summary>
    Failed,

    PartiallyRefunded,

    Refunded
}

public enum CardBrand
{
    Unknown,
    Visa,
    Mastercard,
    Amex,
    Discover
}

/// <summary>One payment attempt. Declined and failed attempts are stored too (used by the fraud check).</summary>
public class PaymentTransaction
{
    public Guid Id { get; set; }

    public string IdempotencyKey { get; set; } = "";

    public string OrderReference { get; set; } = "";

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";

    public CardBrand CardBrand { get; set; }

    public string CardLast4 { get; set; } = "";

    /// <summary>SHA-256 hash of the card number, so attempts with the same card can be found without storing it.</summary>
    public string CardFingerprint { get; set; } = "";

    public string CardholderName { get; set; } = "";

    public string? CustomerEmail { get; set; }

    public PaymentStatus Status { get; set; }

    public string? DeclineCode { get; set; }

    public int FraudScore { get; set; }

    public decimal RefundedAmount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<Refund> Refunds { get; set; } = [];
}

public class Refund
{
    public Guid Id { get; set; }

    public Guid PaymentId { get; set; }

    public decimal Amount { get; set; }

    public string? Reason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
