using PixelVault.Basket.Clients;
using PixelVault.Basket.Domain;
using PixelVault.Basket.Dtos;
using PixelVault.Basket.Repositories;
using PixelVault.Shared.Errors;

namespace PixelVault.Basket.Services;

public interface IBasketService
{
    Task<BasketDto> CreateAsync(CancellationToken cancellationToken = default);

    Task<BasketDto> GetAsync(Guid basketId, CancellationToken cancellationToken = default);

    Task<BasketDto> AddItemAsync(Guid basketId, AddItemRequest request, CancellationToken cancellationToken = default);

    Task<BasketDto> UpdateItemAsync(Guid basketId, int gameId, UpdateItemRequest request, CancellationToken cancellationToken = default);

    Task<BasketDto> RemoveItemAsync(Guid basketId, int gameId, CancellationToken cancellationToken = default);

    Task<BasketDto> ApplyPromoCodeAsync(Guid basketId, ApplyPromoCodeRequest request, CancellationToken cancellationToken = default);

    Task<BasketDto> RemovePromoCodeAsync(Guid basketId, CancellationToken cancellationToken = default);

    /// <summary>Closes the basket after an order was placed. Called by the Order service.</summary>
    Task<BasketDto> CheckoutAsync(Guid basketId, CheckoutBasketRequest request, CancellationToken cancellationToken = default);
}

public class BasketService(
    IBasketRepository baskets,
    IPromoCodeRepository promoCodes,
    ICatalogClient catalog,
    IBasketRules rules,
    IBasketPricingCalculator pricing,
    TimeProvider timeProvider) : IBasketService
{
    public async Task<BasketDto> CreateAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var basket = new ShoppingBasket
        {
            Id = Guid.NewGuid(),
            Status = BasketStatus.Active,
            CreatedAt = now,
            LastActivityAt = now
        };

        baskets.Add(basket);
        await baskets.SaveChangesAsync(cancellationToken);
        return await PriceAsync(basket, cancellationToken);
    }

    public async Task<BasketDto> GetAsync(Guid basketId, CancellationToken cancellationToken = default)
    {
        var basket = await LoadAsync(basketId, cancellationToken);
        if (rules.IsExpired(basket, timeProvider.GetUtcNow()))
        {
            throw new DomainStatusException(410, "BASKET_EXPIRED", "This basket has expired. Please start a new one.");
        }

        return await PriceAsync(basket, cancellationToken);
    }

    public async Task<BasketDto> AddItemAsync(Guid basketId, AddItemRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity < 1)
        {
            throw new ValidationException("quantity", "Quantity must be at least 1.");
        }

        var now = timeProvider.GetUtcNow();
        var basket = await LoadAsync(basketId, cancellationToken);
        rules.EnsureCanBeModified(basket, now);

        var game = await catalog.GetGameAsync(request.GameId, cancellationToken)
                   ?? throw new NotFoundException("Game", request.GameId);

        var existing = basket.Items.FirstOrDefault(i => i.GameId == game.Id);
        var newQuantity = (existing?.Quantity ?? 0) + request.Quantity;
        rules.EnsureQuantityAllowed(basket, game, newQuantity);

        if (existing is null)
        {
            var item = new BasketItem
            {
                BasketId = basket.Id,
                GameId = game.Id,
                Quantity = newQuantity,
                UnitPriceWhenAdded = game.Pricing.Price,
                AddedAt = now
            };
            basket.Items.Add(item);
            baskets.AddItem(item);
        }
        else
        {
            existing.Quantity = newQuantity;
        }

        basket.LastActivityAt = now;
        await baskets.SaveChangesAsync(cancellationToken);
        return await PriceAsync(basket, cancellationToken);
    }

    public async Task<BasketDto> UpdateItemAsync(Guid basketId, int gameId, UpdateItemRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity < 0)
        {
            throw new ValidationException("quantity", "Quantity cannot be negative.");
        }

        if (request.Quantity == 0)
        {
            return await RemoveItemAsync(basketId, gameId, cancellationToken);
        }

        var now = timeProvider.GetUtcNow();
        var basket = await LoadAsync(basketId, cancellationToken);
        rules.EnsureCanBeModified(basket, now);

        var item = basket.Items.FirstOrDefault(i => i.GameId == gameId)
                   ?? throw new NotFoundException("Basket item", gameId);

        if (request.Quantity > item.Quantity)
        {
            var game = await catalog.GetGameAsync(gameId, cancellationToken)
                       ?? throw new NotFoundException("Game", gameId);
            rules.EnsureQuantityAllowed(basket, game, request.Quantity);
        }

        item.Quantity = request.Quantity;
        basket.LastActivityAt = now;
        await baskets.SaveChangesAsync(cancellationToken);
        return await PriceAsync(basket, cancellationToken);
    }

    public async Task<BasketDto> RemoveItemAsync(Guid basketId, int gameId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var basket = await LoadAsync(basketId, cancellationToken);
        rules.EnsureCanBeModified(basket, now);

        var item = basket.Items.FirstOrDefault(i => i.GameId == gameId)
                   ?? throw new NotFoundException("Basket item", gameId);

        basket.Items.Remove(item);
        baskets.RemoveItem(item);
        basket.LastActivityAt = now;
        await baskets.SaveChangesAsync(cancellationToken);
        return await PriceAsync(basket, cancellationToken);
    }

    public async Task<BasketDto> ApplyPromoCodeAsync(Guid basketId, ApplyPromoCodeRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(code))
        {
            throw new ValidationException("code", "Promo code is required.");
        }

        var now = timeProvider.GetUtcNow();
        var basket = await LoadAsync(basketId, cancellationToken);
        rules.EnsureCanBeModified(basket, now);

        if (basket.Items.Count == 0)
        {
            throw new BusinessRuleException("BASKET_EMPTY", "Add something to your basket before using a promo code.");
        }

        if (basket.PromoCode is not null && basket.PromoCode != code)
        {
            throw new BusinessRuleException(
                "PROMO_CODE_ALREADY_APPLIED",
                $"Promo code {basket.PromoCode} is already applied. Only one promo code can be used per order; remove it first.");
        }

        var promoCode = await promoCodes.GetByCodeAsync(code, cancellationToken);
        var basketView = await PriceAsync(basket, cancellationToken, promoCode, code);
        if (basketView.PromoCode is { IsValid: false } status)
        {
            var exception = new BusinessRuleException("INVALID_PROMO_CODE", status.Message);
            exception.Details["reason"] = status.FailureReason;
            throw exception;
        }

        basket.PromoCode = code;
        basket.LastActivityAt = now;
        await baskets.SaveChangesAsync(cancellationToken);
        return await PriceAsync(basket, cancellationToken);
    }

    public async Task<BasketDto> RemovePromoCodeAsync(Guid basketId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var basket = await LoadAsync(basketId, cancellationToken);
        rules.EnsureCanBeModified(basket, now);

        basket.PromoCode = null;
        basket.LastActivityAt = now;
        await baskets.SaveChangesAsync(cancellationToken);
        return await PriceAsync(basket, cancellationToken);
    }

    public async Task<BasketDto> CheckoutAsync(Guid basketId, CheckoutBasketRequest request, CancellationToken cancellationToken = default)
    {
        var orderReference = request.OrderReference?.Trim();
        if (string.IsNullOrEmpty(orderReference))
        {
            throw new ValidationException("orderReference", "Order reference is required.");
        }

        var now = timeProvider.GetUtcNow();
        var basket = await LoadAsync(basketId, cancellationToken);

        if (basket.Status == BasketStatus.CheckedOut)
        {
            if (basket.OrderReference == orderReference)
            {
                return await PriceAsync(basket, cancellationToken);
            }

            throw new ConflictException("BASKET_CHECKED_OUT", "This basket has already been checked out for another order.");
        }

        rules.EnsureCanBeModified(basket, now);
        if (basket.Items.Count == 0)
        {
            throw new BusinessRuleException("BASKET_EMPTY", "An empty basket cannot be checked out.");
        }

        var view = await PriceAsync(basket, cancellationToken);
        if (view.AppliedDiscount == AppliedDiscount.PromoCode && basket.PromoCode is not null)
        {
            var promoCode = await promoCodes.GetByCodeAsync(basket.PromoCode, cancellationToken);
            if (promoCode is not null)
            {
                promoCode.TimesUsed++;
            }
        }

        basket.Status = BasketStatus.CheckedOut;
        basket.CheckedOutAt = now;
        basket.OrderReference = orderReference;
        await baskets.SaveChangesAsync(cancellationToken);
        return view with { Status = BasketStatus.CheckedOut, OrderReference = orderReference, CanCheckout = false };
    }

    private async Task<ShoppingBasket> LoadAsync(Guid basketId, CancellationToken cancellationToken) =>
        await baskets.GetAsync(basketId, cancellationToken) ?? throw new NotFoundException("Basket", basketId);

    private async Task<BasketDto> PriceAsync(ShoppingBasket basket, CancellationToken cancellationToken)
    {
        var promoCode = basket.PromoCode is null ? null : await promoCodes.GetByCodeAsync(basket.PromoCode, cancellationToken);
        return await PriceAsync(basket, cancellationToken, promoCode, basket.PromoCode);
    }

    private async Task<BasketDto> PriceAsync(ShoppingBasket basket, CancellationToken cancellationToken, PromoCode? promoCode, string? code)
    {
        var games = await catalog.GetGamesAsync(basket.Items.Select(i => i.GameId), cancellationToken);
        return pricing.Price(basket, games, code, promoCode, timeProvider.GetUtcNow());
    }
}
