namespace PixelVault.Catalog.Domain;

public class Game
{
    public int Id { get; set; }

    public string Slug { get; set; } = "";

    public string Title { get; set; } = "";

    public string Description { get; set; } = "";

    public string Publisher { get; set; } = "";

    public string Developer { get; set; } = "";

    public Category Category { get; set; }

    public Platform Platforms { get; set; }

    public AgeRating AgeRating { get; set; }

    public DateOnly ReleaseDate { get; set; }

    public decimal BasePrice { get; set; }

    /// <summary>Sale discount in percent (1-90). Only applies between <see cref="SaleStartsAt"/> and <see cref="SaleEndsAt"/>.</summary>
    public int? SalePercentage { get; set; }

    /// <summary>Inclusive start of the sale.</summary>
    public DateTimeOffset? SaleStartsAt { get; set; }

    /// <summary>Exclusive end of the sale. Null means the sale has no end date.</summary>
    public DateTimeOffset? SaleEndsAt { get; set; }

    /// <summary>Units in the warehouse that can still be sold. Reserved units have already been subtracted.</summary>
    public int StockQuantity { get; set; }

    /// <summary>Position in the default "featured" sort order (lowest first).</summary>
    public int FeaturedRank { get; set; }

    /// <summary>Two hex colors used by the frontend to draw the cover art.</summary>
    public string CoverColorPrimary { get; set; } = "#333333";

    public string CoverColorSecondary { get; set; } = "#999999";
}
