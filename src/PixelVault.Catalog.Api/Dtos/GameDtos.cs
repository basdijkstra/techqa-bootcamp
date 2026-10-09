using PixelVault.Catalog.Domain;

namespace PixelVault.Catalog.Dtos;

public record PriceDto(
    decimal BasePrice,
    decimal Price,
    bool IsOnSale,
    int? SalePercentage,
    decimal Savings,
    DateTimeOffset? SaleEndsAt);

public record GameSummaryDto(
    int Id,
    string Slug,
    string Title,
    Category Category,
    IReadOnlyList<Platform> Platforms,
    AgeRating AgeRating,
    int MinimumAge,
    DateOnly ReleaseDate,
    PriceDto Pricing,
    StockStatus StockStatus,
    int AvailableQuantity,
    string CoverColorPrimary,
    string CoverColorSecondary);

public record GameDetailDto(
    int Id,
    string Slug,
    string Title,
    string Description,
    string Publisher,
    string Developer,
    Category Category,
    IReadOnlyList<Platform> Platforms,
    AgeRating AgeRating,
    int MinimumAge,
    DateOnly ReleaseDate,
    PriceDto Pricing,
    StockStatus StockStatus,
    int AvailableQuantity,
    string CoverColorPrimary,
    string CoverColorSecondary);

/// <summary>
/// Query string parameters for <c>GET /games</c>. Values arrive as strings and are validated by the service,
/// so that invalid values produce a clear validation error.
/// </summary>
public class GameSearchQuery
{
    /// <summary>One or more categories, e.g. <c>?category=Rpg&amp;category=Action</c>. Games in any of them match.</summary>
    public List<string> Category { get; set; } = [];

    /// <summary>PC, PlayStation, Xbox or Switch.</summary>
    public string? Platform { get; set; }

    /// <summary>Minimum (sale) price, inclusive.</summary>
    public decimal? MinPrice { get; set; }

    /// <summary>Maximum (sale) price, inclusive.</summary>
    public decimal? MaxPrice { get; set; }

    /// <summary>Only games that are on sale right now.</summary>
    public bool OnSale { get; set; }

    /// <summary>Only games that can ship now (InStock or LowStock). Excludes pre-orders.</summary>
    public bool InStockOnly { get; set; }

    /// <summary>Searches title, publisher and developer. Ignored when shorter than 2 characters.</summary>
    public string? Search { get; set; }

    /// <summary>featured (default), price-asc, price-desc, title, newest.</summary>
    public string? Sort { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 12;
}

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public record CategoryDto(Category Name, string DisplayName, int GameCount);
