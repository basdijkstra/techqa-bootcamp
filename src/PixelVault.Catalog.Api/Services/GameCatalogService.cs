using PixelVault.Catalog.Domain;
using PixelVault.Catalog.Dtos;
using PixelVault.Catalog.Repositories;
using PixelVault.Shared.Errors;

namespace PixelVault.Catalog.Services;

public interface IGameCatalogService
{
    Task<PagedResult<GameSummaryDto>> SearchAsync(GameSearchQuery query, CancellationToken cancellationToken = default);

    Task<GameDetailDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<GameDetailDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GameSummaryDto>> GetManyAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
}

public class GameCatalogService(IGameRepository games, IGameMapper mapper, TimeProvider timeProvider) : IGameCatalogService
{
    public const int MaxPageSize = 50;
    public const int MaxBatchSize = 50;
    public const int MinSearchLength = 2;
    public const int MaxSearchLength = 100;

    private static readonly string[] SortOptions = ["featured", "price-asc", "price-desc", "title", "newest"];

    public async Task<PagedResult<GameSummaryDto>> SearchAsync(GameSearchQuery query, CancellationToken cancellationToken = default)
    {
        var criteria = Validate(query);
        var now = timeProvider.GetUtcNow();

        var all = await games.GetAllAsync(cancellationToken);
        var items = all.Select(game => (Game: game, Dto: mapper.ToSummary(game, now)));

        if (criteria.Categories.Count > 0)
        {
            items = items.Where(i => criteria.Categories.Contains(i.Game.Category));
        }

        if (criteria.Platform is { } platform)
        {
            items = items.Where(i => i.Game.Platforms.HasFlag(platform));
        }

        if (query.MinPrice is { } minPrice)
        {
            items = items.Where(i => i.Dto.Pricing.Price >= minPrice);
        }

        if (query.MaxPrice is { } maxPrice)
        {
            items = items.Where(i => i.Dto.Pricing.Price <= maxPrice);
        }

        if (query.OnSale)
        {
            items = items.Where(i => i.Dto.Pricing.IsOnSale);
        }

        if (query.InStockOnly)
        {
            items = items.Where(i => i.Dto.StockStatus is StockStatus.InStock or StockStatus.LowStock);
        }

        if (criteria.Search is { } search)
        {
            items = items.Where(i =>
                i.Game.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                || i.Game.Publisher.Contains(search, StringComparison.OrdinalIgnoreCase)
                || i.Game.Developer.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        items = criteria.Sort switch
        {
            "price-asc" => items.OrderBy(i => i.Dto.Pricing.Price).ThenBy(i => i.Game.Title),
            "price-desc" => items.OrderByDescending(i => i.Dto.Pricing.Price).ThenBy(i => i.Game.Title),
            "title" => items.OrderBy(i => i.Game.Title, StringComparer.OrdinalIgnoreCase),
            "newest" => items.OrderByDescending(i => i.Game.ReleaseDate).ThenBy(i => i.Game.Title),
            _ => items.OrderBy(i => i.Game.FeaturedRank).ThenBy(i => i.Game.Title)
        };

        var matches = items.Select(i => i.Dto).ToList();
        var totalPages = (int)Math.Ceiling(matches.Count / (double)query.PageSize);
        var page = matches
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new PagedResult<GameSummaryDto>(page, query.Page, query.PageSize, matches.Count, totalPages);
    }

    public async Task<GameDetailDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var game = await games.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Game", id);
        return mapper.ToDetail(game, timeProvider.GetUtcNow());
    }

    public async Task<GameDetailDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var game = await games.GetBySlugAsync(slug.Trim().ToLowerInvariant(), cancellationToken)
                   ?? throw new NotFoundException("Game", slug);
        return mapper.ToDetail(game, timeProvider.GetUtcNow());
    }

    public async Task<IReadOnlyList<GameSummaryDto>> GetManyAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            throw new ValidationException("ids", "At least one id is required.");
        }

        if (ids.Count > MaxBatchSize)
        {
            throw new ValidationException("ids", $"At most {MaxBatchSize} ids can be requested at once.");
        }

        var now = timeProvider.GetUtcNow();
        var found = await games.GetByIdsAsync(ids, cancellationToken);
        return found.OrderBy(g => g.Id).Select(g => mapper.ToSummary(g, now)).ToList();
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var all = await games.GetAllAsync(cancellationToken);
        return Enum.GetValues<Category>()
            .Select(c => new CategoryDto(c, Categories.DisplayName(c), all.Count(g => g.Category == c)))
            .ToList();
    }

    private sealed record SearchCriteria(IReadOnlyList<Category> Categories, Platform? Platform, string? Search, string Sort);

    private static SearchCriteria Validate(GameSearchQuery query)
    {
        var errors = new ValidationErrors();

        var categories = new List<Category>();
        foreach (var value in query.Category.SelectMany(c => c.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
        {
            if (Enum.TryParse<Category>(value, ignoreCase: true, out var category) && Enum.IsDefined(category))
            {
                categories.Add(category);
            }
            else
            {
                errors.Add("category", $"Unknown category '{value}'.");
            }
        }

        Platform? platform = null;
        if (!string.IsNullOrWhiteSpace(query.Platform))
        {
            if (Enum.TryParse<Platform>(query.Platform.Trim(), ignoreCase: true, out var parsed)
                && parsed != Platform.None && Enum.IsDefined(parsed))
            {
                platform = parsed;
            }
            else
            {
                errors.Add("platform", $"Unknown platform '{query.Platform}'.");
            }
        }

        errors.AddIf(query.MinPrice < 0, "minPrice", "Minimum price cannot be negative.");
        errors.AddIf(query.MaxPrice < 0, "maxPrice", "Maximum price cannot be negative.");
        errors.AddIf(query.MinPrice > query.MaxPrice, "minPrice", "Minimum price cannot be higher than maximum price.");
        errors.AddIf(query.Page < 1, "page", "Page must be 1 or higher.");
        errors.AddIf(query.PageSize is < 1 or > MaxPageSize, "pageSize", $"Page size must be between 1 and {MaxPageSize}.");

        var search = query.Search?.Trim();
        errors.AddIf(search?.Length > MaxSearchLength, "search", $"Search text cannot be longer than {MaxSearchLength} characters.");
        if (search is not null && search.Length < MinSearchLength)
        {
            search = null;
        }

        var sort = string.IsNullOrWhiteSpace(query.Sort) ? "featured" : query.Sort.Trim().ToLowerInvariant();
        errors.AddIf(!SortOptions.Contains(sort), "sort", $"Sort must be one of: {string.Join(", ", SortOptions)}.");

        errors.ThrowIfAny();
        return new SearchCriteria(categories.Distinct().ToList(), platform, search, sort);
    }
}
