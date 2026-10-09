using Microsoft.AspNetCore.Mvc;
using PixelVault.Payment.Dtos;
using PixelVault.Payment.Services;

namespace PixelVault.Payment.Controllers;

/// <summary>A fake payment provider. Called by the Order service during checkout.</summary>
[ApiController]
[Route("payments")]
[Produces("application/json")]
public class PaymentsController(IPaymentService payments) : ControllerBase
{
    /// <summary>
    /// Charges a card. Requires an <c>Idempotency-Key</c> header. Returns 201 for a new payment, 200 when the same
    /// request is repeated, 402 when the card is declined and 502 when the processor fails.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<PaymentDto>> Create(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var (payment, created) = await payments.CreatePaymentAsync(idempotencyKey, request, cancellationToken);
        return created ? CreatedAtAction(nameof(Get), new { paymentId = payment.Id }, payment) : Ok(payment);
    }

    [HttpGet("{paymentId:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentDto>> Get(Guid paymentId, CancellationToken cancellationToken) =>
        Ok(await payments.GetAsync(paymentId, cancellationToken));

    /// <summary>All payment attempts for an order.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> GetByOrder([FromQuery] string orderReference, CancellationToken cancellationToken) =>
        Ok(await payments.GetByOrderReferenceAsync(orderReference, cancellationToken));

    /// <summary>Refunds part or all of a captured payment.</summary>
    [HttpPost("{paymentId:guid}/refunds")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<RefundDto>> Refund(Guid paymentId, CreateRefundRequest request, CancellationToken cancellationToken)
    {
        var refund = await payments.RefundAsync(paymentId, request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { paymentId }, refund);
    }
}

/// <summary>Fault injection. Only available when <c>TestSupport:Enabled</c> is true.</summary>
[ApiController]
[Route("test-support/faults")]
[Produces("application/json")]
[Tags("Test support")]
public class FaultInjectionController(IFaultInjector faultInjector, IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public ActionResult<FaultInjectionSettingsDto> Get() =>
        Enabled ? Ok(faultInjector.Settings) : NotFound();

    /// <summary>
    /// Makes payments slow or fail at random. For example <c>{"failureRate": 0.5, "extraLatencyMilliseconds": 2000}</c>
    /// fails half of all payments after a 2 second delay. Set both to 0 to switch it off.
    /// </summary>
    [HttpPut]
    public ActionResult<FaultInjectionSettingsDto> Put(FaultInjectionSettingsDto settings)
    {
        if (!Enabled)
        {
            return NotFound();
        }

        faultInjector.Configure(settings);
        return Ok(faultInjector.Settings);
    }

    private bool Enabled => configuration.GetValue("TestSupport:Enabled", false);
}
