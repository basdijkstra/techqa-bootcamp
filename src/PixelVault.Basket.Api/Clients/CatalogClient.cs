using System.Net;
using PixelVault.Shared.Errors;

namespace PixelVault.Basket.Clients;

public record CatalogPricing(decimal BasePrice, decimal Price, bool IsOnSale);

/// <summary>The parts of a Catalog game that the Basket service needs.</summary>
public record CatalogGame(
    int Id,
    string Slug,
    string Title,
    string Category,
    string AgeRating,
    int MinimumAge,
    DateOnly ReleaseDate,
    CatalogPricing Pricing,
    string StockStatus,
    int AvailableQuantity,
    string CoverColorPrimary,
    string CoverColorSecondary)
{
    public bool IsPreOrder => StockStatus == "PreOrder";

    public bool IsOutOfStock => StockStatus == "OutOfStock";
}

public interface ICatalogClient
{
    /// <summary>Gets a game, or null when it doesn't exist.</summary>
    Task<CatalogGame?> GetGameAsync(int gameId, CancellationToken cancellationToken = default);

    /// <summary>Gets several games. Games that don't exist are left out.</summary>
    Task<IReadOnlyList<CatalogGame>> GetGamesAsync(IEnumerable<int> gameIds, CancellationToken cancellationToken = default);
}

public class CatalogClient(HttpClient httpClient) : ICatalogClient
{
    private const string ServiceName = "Catalog";

    public async Task<CatalogGame?> GetGameAsync(int gameId, CancellationToken cancellationToken = default)
    {
        using var response = await DownstreamCall.SendAsync(ServiceName, () => httpClient.GetAsync($"games/{gameId}", cancellationToken));
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await response.EnsureSuccessOrThrowAsync(ServiceName, cancellationToken);
        return await response.Content.ReadFromJsonAsync<CatalogGame>(cancellationToken);
    }

    public async Task<IReadOnlyList<CatalogGame>> GetGamesAsync(IEnumerable<int> gameIds, CancellationToken cancellationToken = default)
    {
        var ids = gameIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var query = string.Join("&", ids.Select(id => $"ids={id}"));
        using var response = await DownstreamCall.SendAsync(ServiceName, () => httpClient.GetAsync($"games/batch?{query}", cancellationToken));
        await response.EnsureSuccessOrThrowAsync(ServiceName, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<CatalogGame>>(cancellationToken) ?? [];
    }
}
