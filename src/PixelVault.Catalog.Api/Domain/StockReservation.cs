namespace PixelVault.Catalog.Domain;

/// <summary>
/// Stock held for an order during checkout. One row per game per order.
/// </summary>
public class StockReservation
{
    public int Id { get; set; }

    public string OrderReference { get; set; } = "";

    public int GameId { get; set; }

    public int Quantity { get; set; }

    /// <summary>False for pre-orders: they are reserved, but do not take units out of stock.</summary>
    public bool ConsumesStock { get; set; }

    public ReservationStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
