using PixelVault.Basket.Clients;
using PixelVault.Basket.Domain;
using PixelVault.Shared.Errors;

namespace PixelVault.Basket.Services;

public interface IBasketRules
{
    /// <summary>Throws when the basket can no longer be changed (checked out or expired).</summary>
    void EnsureCanBeModified(ShoppingBasket basket, DateTimeOffset now);

    bool IsExpired(ShoppingBasket basket, DateTimeOffset now);

    /// <summary>Throws when the basket cannot hold <paramref name="newQuantity"/> units of <paramref name="game"/>.</summary>
    void EnsureQuantityAllowed(ShoppingBasket basket, CatalogGame game, int newQuantity);
}

/// <summary>
/// Basket limits:
/// <list type="bullet">
/// <item>Baskets expire 7 days after the last change. A checked out basket cannot be changed.</item>
/// <item>Out-of-stock games cannot be added.</item>
/// <item>At most 5 units per game, and at most 2 per game for pre-orders.</item>
/// <item>The quantity cannot be higher than the units in stock (pre-orders excepted).</item>
/// <item>At most 10 units in the whole basket.</item>
/// </list>
/// </summary>
public class BasketRules : IBasketRules
{
    public const int ExpiryDays = 7;
    public const int MaxQuantityPerGame = 5;
    public const int MaxPreOrderQuantityPerGame = 2;
    public const int MaxUnitsPerBasket = 10;

    public bool IsExpired(ShoppingBasket basket, DateTimeOffset now) =>
        basket.Status == BasketStatus.Active && now >= basket.LastActivityAt.AddDays(ExpiryDays);

    public void EnsureCanBeModified(ShoppingBasket basket, DateTimeOffset now)
    {
        if (basket.Status == BasketStatus.CheckedOut)
        {
            throw new ConflictException("BASKET_CHECKED_OUT", "This basket has already been checked out.");
        }

        if (IsExpired(basket, now))
        {
            throw new DomainStatusException(410, "BASKET_EXPIRED", "This basket has expired. Please start a new one.");
        }
    }

    public void EnsureQuantityAllowed(ShoppingBasket basket, CatalogGame game, int newQuantity)
    {
        if (game.IsOutOfStock)
        {
            throw new ConflictException("OUT_OF_STOCK", $"'{game.Title}' is out of stock.");
        }

        if (game.IsPreOrder && newQuantity > MaxPreOrderQuantityPerGame)
        {
            throw new BusinessRuleException(
                "PREORDER_LIMIT_EXCEEDED",
                $"You can pre-order at most {MaxPreOrderQuantityPerGame} copies of '{game.Title}'.");
        }

        if (newQuantity > MaxQuantityPerGame)
        {
            throw new BusinessRuleException(
                "QUANTITY_LIMIT_EXCEEDED",
                $"You can buy at most {MaxQuantityPerGame} copies of '{game.Title}'.");
        }

        if (!game.IsPreOrder && newQuantity > game.AvailableQuantity)
        {
            var exception = new ConflictException(
                "INSUFFICIENT_STOCK",
                $"Only {game.AvailableQuantity} copies of '{game.Title}' are in stock.");
            exception.Details["available"] = game.AvailableQuantity;
            throw exception;
        }

        var otherUnits = basket.Items.Where(i => i.GameId != game.Id).Sum(i => i.Quantity);
        if (otherUnits + newQuantity > MaxUnitsPerBasket)
        {
            throw new BusinessRuleException(
                "BASKET_LIMIT_EXCEEDED",
                $"A basket can hold at most {MaxUnitsPerBasket} items.");
        }
    }
}
