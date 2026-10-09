using PixelVault.Catalog.Domain;
using PixelVault.Catalog.Dtos;

namespace PixelVault.Catalog.Services;

public interface IGameMapper
{
    GameSummaryDto ToSummary(Game game, DateTimeOffset now);

    GameDetailDto ToDetail(Game game, DateTimeOffset now);
}

public class GameMapper(IPricingCalculator pricing, IStockStatusResolver stockStatus) : IGameMapper
{
    public GameSummaryDto ToSummary(Game game, DateTimeOffset now) => new(
        game.Id,
        game.Slug,
        game.Title,
        game.Category,
        PlatformList(game.Platforms),
        game.AgeRating,
        AgeRatings.MinimumAge(game.AgeRating),
        game.ReleaseDate,
        pricing.Calculate(game, now),
        stockStatus.Resolve(game, DateOnly.FromDateTime(now.UtcDateTime)),
        Math.Max(game.StockQuantity, 0),
        game.CoverColorPrimary,
        game.CoverColorSecondary);

    public GameDetailDto ToDetail(Game game, DateTimeOffset now)
    {
        var summary = ToSummary(game, now);
        return new GameDetailDto(
            summary.Id,
            summary.Slug,
            summary.Title,
            game.Description,
            game.Publisher,
            game.Developer,
            summary.Category,
            summary.Platforms,
            summary.AgeRating,
            summary.MinimumAge,
            summary.ReleaseDate,
            summary.Pricing,
            summary.StockStatus,
            summary.AvailableQuantity,
            summary.CoverColorPrimary,
            summary.CoverColorSecondary);
    }

    public static IReadOnlyList<Platform> PlatformList(Platform platforms) =>
        Enum.GetValues<Platform>()
            .Where(p => p != Platform.None && platforms.HasFlag(p))
            .ToList();
}

public static class AgeRatings
{
    /// <summary>Minimum customer age for an ESRB rating. Only M (17) and AO (18) are checked at checkout.</summary>
    public static int MinimumAge(AgeRating rating) => rating switch
    {
        AgeRating.E => 0,
        AgeRating.E10Plus => 10,
        AgeRating.T => 13,
        AgeRating.M => 17,
        AgeRating.AO => 18,
        _ => throw new ArgumentOutOfRangeException(nameof(rating), rating, null)
    };
}

public static class Categories
{
    public static string DisplayName(Category category) => category switch
    {
        Category.Rpg => "RPG",
        _ => category.ToString()
    };
}
