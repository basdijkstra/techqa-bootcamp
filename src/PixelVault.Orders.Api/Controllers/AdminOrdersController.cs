using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixelVault.Orders.Dtos;
using PixelVault.Orders.Services;
using PixelVault.Shared.Auth;

namespace PixelVault.Orders.Controllers;

[ApiController]
[Route("admin/orders")]
[Produces("application/json")]
[Authorize(Policy = Policies.Staff)]
public class AdminOrdersController(IAdminOrderService adminOrders) : ControllerBase
{
    /// <summary>Lists orders, newest first. Admin or Support.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderListItemDto>>> List([FromQuery] OrderListQuery query, CancellationToken cancellationToken) =>
        Ok(await adminOrders.ListAsync(query, cancellationToken));

    /// <summary>Order counts and revenue. Admin or Support.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<OrderSummaryDto>> Summary(CancellationToken cancellationToken) =>
        Ok(await adminOrders.GetSummaryAsync(cancellationToken));

    /// <summary>One order with its status history and the status changes that are allowed. Admin or Support.</summary>
    [HttpGet("{orderNumber}")]
    public async Task<ActionResult<AdminOrderDto>> Get(string orderNumber, CancellationToken cancellationToken) =>
        Ok(await adminOrders.GetAsync(orderNumber, cancellationToken));

    /// <summary>Ships, delivers, cancels or refunds an order. Admin only.</summary>
    [HttpPost("{orderNumber}/status")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AdminOrderDto>> ChangeStatus(string orderNumber, ChangeOrderStatusRequest request, CancellationToken cancellationToken) =>
        Ok(await adminOrders.ChangeStatusAsync(orderNumber, request, User.GetUsername(), cancellationToken));
}
