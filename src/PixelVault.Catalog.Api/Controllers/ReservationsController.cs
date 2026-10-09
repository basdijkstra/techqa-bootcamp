using Microsoft.AspNetCore.Mvc;
using PixelVault.Catalog.Dtos;
using PixelVault.Catalog.Services;

namespace PixelVault.Catalog.Controllers;

/// <summary>Stock reservations. Used by the Order service during checkout.</summary>
[ApiController]
[Route("reservations")]
[Produces("application/json")]
public class ReservationsController(IReservationService reservations) : ControllerBase
{
    /// <summary>Reserves stock for an order. Returns 201 for a new reservation, 200 if it already existed.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationDto>> Reserve(CreateReservationRequest request, CancellationToken cancellationToken)
    {
        var (reservation, created) = await reservations.ReserveAsync(request, cancellationToken);
        return created
            ? CreatedAtAction(nameof(Get), new { orderReference = reservation.OrderReference }, reservation)
            : Ok(reservation);
    }

    [HttpGet("{orderReference}")]
    public async Task<ActionResult<ReservationDto>> Get(string orderReference, CancellationToken cancellationToken) =>
        Ok(await reservations.GetAsync(orderReference, cancellationToken));

    /// <summary>Makes the reservation final after a successful payment.</summary>
    [HttpPost("{orderReference}/commit")]
    public async Task<ActionResult<ReservationDto>> Commit(string orderReference, CancellationToken cancellationToken) =>
        Ok(await reservations.CommitAsync(orderReference, cancellationToken));

    /// <summary>Puts the reserved units back in stock.</summary>
    [HttpPost("{orderReference}/release")]
    public async Task<ActionResult<ReservationDto>> Release(string orderReference, CancellationToken cancellationToken) =>
        Ok(await reservations.ReleaseAsync(orderReference, cancellationToken));
}
