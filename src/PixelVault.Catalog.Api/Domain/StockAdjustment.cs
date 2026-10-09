namespace PixelVault.Catalog.Domain;

/// <summary>Audit record for every manual stock change made by staff.</summary>
public class StockAdjustment
{
    public int Id { get; set; }

    public int GameId { get; set; }

    public int QuantityChange { get; set; }

    public int QuantityBefore { get; set; }

    public int QuantityAfter { get; set; }

    public StockAdjustmentReason Reason { get; set; }

    public string? Note { get; set; }

    public string PerformedBy { get; set; } = "";

    public DateTimeOffset PerformedAt { get; set; }
}
