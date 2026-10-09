using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixelVault.Catalog.Dtos;
using PixelVault.Catalog.Services;
using PixelVault.Shared.Auth;

namespace PixelVault.Catalog.Controllers;

[ApiController]
[Route("admin")]
[Produces("application/json")]
[Authorize(Policy = Policies.Staff)]
public class AdminStockController(IStockService stock) : ControllerBase
{
    /// <summary>Stock levels for every game. Admin or Support.</summary>
    [HttpGet("stock")]
    public async Task<ActionResult<IReadOnlyList<StockItemDto>>> GetStock(CancellationToken cancellationToken) =>
        Ok(await stock.GetStockOverviewAsync(cancellationToken));

    /// <summary>History of manual stock changes for one game, newest first. Admin or Support.</summary>
    [HttpGet("games/{gameId:int}/stock-adjustments")]
    public async Task<ActionResult<IReadOnlyList<StockAdjustmentDto>>> GetHistory(int gameId, CancellationToken cancellationToken) =>
        Ok(await stock.GetAdjustmentHistoryAsync(gameId, cancellationToken));

    /// <summary>Adds or removes stock for one game. Admin only.</summary>
    [HttpPost("games/{gameId:int}/stock-adjustments")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<StockAdjustmentDto>> Adjust(int gameId, AdjustStockRequest request, CancellationToken cancellationToken)
    {
        var adjustment = await stock.AdjustStockAsync(gameId, request, User.GetUsername(), cancellationToken);
        return CreatedAtAction(nameof(GetHistory), new { gameId }, adjustment);
    }
}
