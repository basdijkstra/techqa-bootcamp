using Microsoft.AspNetCore.Mvc;
using PixelVault.Basket.Dtos;
using PixelVault.Basket.Services;

namespace PixelVault.Basket.Controllers;

[ApiController]
[Route("baskets")]
[Produces("application/json")]
public class BasketsController(IBasketService baskets) : ControllerBase
{
    /// <summary>Creates a new, empty basket.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<BasketDto>> Create(CancellationToken cancellationToken)
    {
        var basket = await baskets.CreateAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { basketId = basket.Id }, basket);
    }

    /// <summary>Gets a basket with current prices, discounts and stock issues.</summary>
    [HttpGet("{basketId:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<ActionResult<BasketDto>> Get(Guid basketId, CancellationToken cancellationToken) =>
        Ok(await baskets.GetAsync(basketId, cancellationToken));

    /// <summary>Adds a game. If the game is already in the basket, the quantities are added up.</summary>
    [HttpPost("{basketId:guid}/items")]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BasketDto>> AddItem(Guid basketId, AddItemRequest request, CancellationToken cancellationToken) =>
        Ok(await baskets.AddItemAsync(basketId, request, cancellationToken));

    /// <summary>Sets the quantity of a game. A quantity of 0 removes it.</summary>
    [HttpPut("{basketId:guid}/items/{gameId:int}")]
    public async Task<ActionResult<BasketDto>> UpdateItem(Guid basketId, int gameId, UpdateItemRequest request, CancellationToken cancellationToken) =>
        Ok(await baskets.UpdateItemAsync(basketId, gameId, request, cancellationToken));

    [HttpDelete("{basketId:guid}/items/{gameId:int}")]
    public async Task<ActionResult<BasketDto>> RemoveItem(Guid basketId, int gameId, CancellationToken cancellationToken) =>
        Ok(await baskets.RemoveItemAsync(basketId, gameId, cancellationToken));

    /// <summary>Applies a promo code. Only one code per basket.</summary>
    [HttpPut("{basketId:guid}/promo-code")]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BasketDto>> ApplyPromoCode(Guid basketId, ApplyPromoCodeRequest request, CancellationToken cancellationToken) =>
        Ok(await baskets.ApplyPromoCodeAsync(basketId, request, cancellationToken));

    [HttpDelete("{basketId:guid}/promo-code")]
    public async Task<ActionResult<BasketDto>> RemovePromoCode(Guid basketId, CancellationToken cancellationToken) =>
        Ok(await baskets.RemovePromoCodeAsync(basketId, cancellationToken));

    /// <summary>Closes the basket after an order was placed. Called by the Order service.</summary>
    [HttpPost("{basketId:guid}/checkout")]
    public async Task<ActionResult<BasketDto>> Checkout(Guid basketId, CheckoutBasketRequest request, CancellationToken cancellationToken) =>
        Ok(await baskets.CheckoutAsync(basketId, request, cancellationToken));
}
