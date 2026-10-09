namespace PixelVault.Basket.Domain;

public enum BasketStatus
{
    Active,
    CheckedOut
}

/// <summary>An anonymous shopping basket. The browser keeps the id in local storage.</summary>
public class ShoppingBasket
{
    public Guid Id { get; set; }

    public BasketStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Last time items or the promo code changed. Baskets expire 7 days after this.</summary>
    public DateTimeOffset LastActivityAt { get; set; }

    /// <summary>Promo code entered by the customer, stored in upper case.</summary>
    public string? PromoCode { get; set; }

    public DateTimeOffset? CheckedOutAt { get; set; }

    /// <summary>Order number of the order that was placed with this basket.</summary>
    public string? OrderReference { get; set; }

    public List<BasketItem> Items { get; set; } = [];
}

public class BasketItem
{
    public int Id { get; set; }

    public Guid BasketId { get; set; }

    public int GameId { get; set; }

    public int Quantity { get; set; }

    /// <summary>Price when the game was first added, used to tell the customer that the price changed.</summary>
    public decimal UnitPriceWhenAdded { get; set; }

    public DateTimeOffset AddedAt { get; set; }
}
