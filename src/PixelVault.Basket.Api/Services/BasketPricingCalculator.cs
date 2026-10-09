using PixelVault.Basket.Clients;
using PixelVault.Basket.Domain;
using PixelVault.Basket.Dtos;

namespace PixelVault.Basket.Services;

public interface IBasketPricingCalculator
{
    /// <summary>
    /// Builds the full basket view: current prices, stock issues, the best discount and the total.
    /// </summary>
    /// <param name="basket">The basket.</param>
    /// <param name="games">Current Catalog data for the games in the basket.</param>
    /// <param name="enteredCode">The promo code the customer entered, or null.</param>
    /// <param name="promoCode">The promo code found for <paramref name="enteredCode"/>, or null if it doesn't exist.</param>
    /// <param name="now">The current time.</param>
    BasketDto Price(ShoppingBasket basket, IReadOnlyCollection<CatalogGame> games, string? enteredCode, PromoCode? promoCode, DateTimeOffset now);
}

/// <summary>
/// Basket pricing rules:
/// <list type="bullet">
/// <item>Lines always use the current Catalog price. If it differs from the price when the game was added, the line is flagged.</item>
/// <item>A line has an issue when the game no longer exists, is out of stock, or has fewer units than the quantity.</item>
/// <item>The bundle discount and the promo code don't stack: the bigger one is applied. On a tie the promo code wins.</item>
/// <item>The discount never makes the total negative.</item>
/// <item>A basket can be checked out when it is active, not empty and has no issues.</item>
/// </list>
/// </summary>
public class BasketPricingCalculator(
    IBundleDiscountCalculator bundleCalculator,
    IPromoCodeEvaluator promoEvaluator) : IBasketPricingCalculator
{
    public BasketDto Price(ShoppingBasket basket, IReadOnlyCollection<CatalogGame> games, string? enteredCode, PromoCode? promoCode, DateTimeOffset now)
    {
        var gamesById = games.ToDictionary(g => g.Id);
        var lines = basket.Items
            .OrderBy(i => i.AddedAt)
            .ThenBy(i => i.Id)
            .Select(item => ToLine(item, gamesById.GetValueOrDefault(item.GameId)))
            .ToList();

        var pricedLines = lines
            .Select(l => new PricedLine(l.GameId, l.Category, l.IsPreOrder, l.Quantity, l.UnitPrice))
            .ToList();

        var subtotal = pricedLines.Sum(l => l.LineTotal);
        var bundle = bundleCalculator.Calculate(pricedLines);
        var messages = new List<string>();

        PromoCodeStatusDto? promoStatus = null;
        var applied = AppliedDiscount.None;
        var discount = 0m;
        string? discountDescription = null;

        PromoEvaluation? promo = null;
        if (enteredCode is not null)
        {
            promo = promoEvaluator.Evaluate(promoCode, pricedLines, now);
        }

        if (promo is { IsValid: true } && promo.Discount >= bundle.Discount && promo.Discount > 0)
        {
            applied = AppliedDiscount.PromoCode;
            discount = promo.Discount;
            discountDescription = $"Promo code {enteredCode}";
        }
        else if (bundle.Discount > 0)
        {
            applied = AppliedDiscount.Bundle;
            discount = bundle.Discount;
            discountDescription = "Bundle deal: buy 3, get the cheapest 50% off";
            if (promo is { IsValid: true })
            {
                messages.Add($"Your bundle discount is bigger than promo code {enteredCode}, so the bundle discount is used.");
            }
        }

        discount = Math.Min(discount, subtotal);

        if (enteredCode is not null && promo is not null)
        {
            var message = applied == AppliedDiscount.PromoCode || !promo.IsValid
                ? promo.Message
                : "Promo code not applied: the bundle discount is bigger.";
            promoStatus = new PromoCodeStatusDto(
                enteredCode,
                promoCode?.Description,
                promo.IsValid,
                applied == AppliedDiscount.PromoCode,
                applied == AppliedDiscount.PromoCode ? discount : 0m,
                promo.FailureReason?.ToString(),
                message);
        }

        var hasIssues = lines.Any(l => l.Issues.Count > 0);
        if (hasIssues)
        {
            messages.Add("Some items in your basket need attention before you can check out.");
        }

        if (lines.Any(l => l.PriceChanged))
        {
            messages.Add("The price of one or more items has changed since you added them.");
        }

        return new BasketDto(
            basket.Id,
            basket.Status,
            basket.CreatedAt,
            basket.LastActivityAt,
            basket.LastActivityAt.AddDays(BasketRules.ExpiryDays),
            basket.OrderReference,
            lines,
            lines.Sum(l => l.Quantity),
            subtotal,
            bundle.Discount,
            promoStatus,
            applied,
            discount,
            discountDescription,
            subtotal - discount,
            lines.Select(l => l.MinimumAge).DefaultIfEmpty(0).Max(),
            lines.Where(l => l.IsPreOrder).Select(l => l.ReleaseDate).Max(),
            hasIssues,
            basket.Status == BasketStatus.Active && lines.Count > 0 && !hasIssues,
            messages);
    }

    private static BasketLineDto ToLine(BasketItem item, CatalogGame? game)
    {
        if (game is null)
        {
            return new BasketLineDto(
                item.GameId, "", "Unavailable game", "", "E", 0, false, null, item.Quantity,
                item.UnitPriceWhenAdded, item.UnitPriceWhenAdded, false, item.UnitPriceWhenAdded * item.Quantity,
                "OutOfStock", 0, null, null, ["This game is no longer sold. Please remove it from your basket."]);
        }

        var issues = new List<string>();
        if (game.IsOutOfStock)
        {
            issues.Add("This game is out of stock. Please remove it from your basket.");
        }
        else if (!game.IsPreOrder && item.Quantity > game.AvailableQuantity)
        {
            issues.Add($"Only {game.AvailableQuantity} left in stock. Please lower the quantity.");
        }

        var unitPrice = game.Pricing.Price;
        return new BasketLineDto(
            game.Id,
            game.Slug,
            game.Title,
            game.Category,
            game.AgeRating,
            game.MinimumAge,
            game.IsPreOrder,
            game.ReleaseDate,
            item.Quantity,
            unitPrice,
            item.UnitPriceWhenAdded,
            unitPrice != item.UnitPriceWhenAdded,
            unitPrice * item.Quantity,
            game.StockStatus,
            game.AvailableQuantity,
            game.CoverColorPrimary,
            game.CoverColorSecondary,
            issues);
    }
}
