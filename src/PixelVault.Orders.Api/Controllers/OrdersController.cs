using Microsoft.AspNetCore.Mvc;
using PixelVault.Orders.Dtos;
using PixelVault.Orders.Services;

namespace PixelVault.Orders.Controllers;

[ApiController]
[Route("orders")]
[Produces("application/json")]
public class OrdersController(ICheckoutService checkout, IOrderService orders) : ControllerBase
{
    /// <summary>Works out shipping, sales tax and the total for a basket and shipping quote, without ordering.</summary>
    [HttpPost("preview")]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderPreviewDto>> Preview(PreviewOrderRequest request, CancellationToken cancellationToken) =>
        Ok(await checkout.PreviewAsync(request, cancellationToken));

    /// <summary>Places an order: reserves stock, charges the card and closes the basket.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<OrderDto>> PlaceOrder(PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await checkout.PlaceOrderAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { orderNumber = order.OrderNumber, email = order.CustomerEmail }, order);
    }

    /// <summary>Looks up an order. The email address used for the order is required.</summary>
    [HttpGet("{orderNumber}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> Get(string orderNumber, [FromQuery] string? email, CancellationToken cancellationToken) =>
        Ok(await orders.GetForCustomerAsync(orderNumber, email, cancellationToken));

    /// <summary>Cancels an order. Possible until it ships, and at most 24 hours after payment.</summary>
    [HttpPost("{orderNumber}/cancel")]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderDto>> Cancel(string orderNumber, CancelOrderRequest request, CancellationToken cancellationToken) =>
        Ok(await orders.CancelByCustomerAsync(orderNumber, request, cancellationToken));
}
