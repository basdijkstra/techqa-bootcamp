namespace PixelVault.Catalog.Domain;

public enum Category
{
    Action,
    Adventure,
    Rpg,
    Strategy,
    Sports,
    Racing,
    Puzzle,
    Simulation,
    Horror,
    Platformer
}

/// <summary>Platforms a game is available on. A game can be on several platforms, so this is a flags enum.</summary>
[Flags]
public enum Platform
{
    None = 0,
    PC = 1,
    PlayStation = 2,
    Xbox = 4,
    Switch = 8
}

/// <summary>ESRB age ratings.</summary>
public enum AgeRating
{
    /// <summary>Everyone.</summary>
    E,

    /// <summary>Everyone 10+.</summary>
    E10Plus,

    /// <summary>Teen (13+).</summary>
    T,

    /// <summary>Mature (17+).</summary>
    M,

    /// <summary>Adults Only (18+).</summary>
    AO
}

public enum StockStatus
{
    InStock,
    LowStock,
    OutOfStock,
    PreOrder
}

public enum StockAdjustmentReason
{
    /// <summary>New stock arrived from the supplier. Quantity change must be positive.</summary>
    Restock,

    /// <summary>A customer returned an item that can be sold again. Quantity change must be positive.</summary>
    Return,

    /// <summary>Items were damaged. Quantity change must be negative.</summary>
    Damaged,

    /// <summary>Items went missing. Quantity change must be negative.</summary>
    Lost,

    /// <summary>A stock count found a difference. Either direction, but a note is required.</summary>
    Correction
}

public enum ReservationStatus
{
    Active,
    Committed,
    Released
}
