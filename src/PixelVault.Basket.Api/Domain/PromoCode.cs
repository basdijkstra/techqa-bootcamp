namespace PixelVault.Basket.Domain;

public enum PromoCodeType
{
    /// <summary>Percentage off the eligible items, e.g. 10 means 10% off.</summary>
    Percentage,

    /// <summary>Fixed dollar amount off the eligible items, e.g. 5 means $5.00 off.</summary>
    FixedAmount
}

public class PromoCode
{
    public int Id { get; set; }

    /// <summary>The code customers type in, stored in upper case.</summary>
    public string Code { get; set; } = "";

    public string Description { get; set; } = "";

    public PromoCodeType Type { get; set; }

    public decimal Value { get; set; }

    /// <summary>Basket subtotal needed before the code can be used. Null means no minimum.</summary>
    public decimal? MinimumSpend { get; set; }

    /// <summary>Inclusive start of the period the code can be used.</summary>
    public DateTimeOffset ValidFrom { get; set; }

    /// <summary>Exclusive end of the period the code can be used. Null means it never expires.</summary>
    public DateTimeOffset? ValidUntil { get; set; }

    /// <summary>When set, the discount only applies to games in this catalog category (e.g. "Rpg").</summary>
    public string? RestrictedToCategory { get; set; }

    /// <summary>How many orders can use this code in total. Null means unlimited.</summary>
    public int? MaxUses { get; set; }

    public int TimesUsed { get; set; }

    public bool IsActive { get; set; } = true;
}
