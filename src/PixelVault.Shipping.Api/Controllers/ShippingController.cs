using Microsoft.AspNetCore.Mvc;
using PixelVault.Shipping.Domain;
using PixelVault.Shipping.Dtos;
using PixelVault.Shipping.Services;

namespace PixelVault.Shipping.Controllers;

[ApiController]
[Produces("application/json")]
public class ShippingController(IQuoteService quotes, IAddressValidator addressValidator, IHolidayCalendar holidays) : ControllerBase
{
    /// <summary>Countries PixelVault ships to, with their states or provinces.</summary>
    [HttpGet("countries")]
    public ActionResult<IReadOnlyList<CountryInfo>> GetCountries() => Ok(Countries.All);

    /// <summary>Checks an address without creating a quote. Always returns 200; see <c>isValid</c>.</summary>
    [HttpPost("addresses/validate")]
    public ActionResult<AddressValidationResultDto> ValidateAddress(AddressDto address) =>
        Ok(addressValidator.Validate(address));

    /// <summary>Creates a shipping quote with every available method, its cost and delivery estimate.</summary>
    [HttpPost("quotes")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ShippingQuoteDto>> CreateQuote(CreateQuoteRequest request, CancellationToken cancellationToken)
    {
        var quote = await quotes.CreateQuoteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetQuote), new { quoteId = quote.Id }, quote);
    }

    [HttpGet("quotes/{quoteId:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShippingQuoteDto>> GetQuote(Guid quoteId, CancellationToken cancellationToken) =>
        Ok(await quotes.GetQuoteAsync(quoteId, cancellationToken));

    /// <summary>US federal holidays for a year. No deliveries on the observed dates.</summary>
    [HttpGet("holidays/{year:int}")]
    public ActionResult<IReadOnlyList<HolidayDto>> GetHolidays(int year) =>
        year is < 2000 or > 2100 ? BadRequest() : Ok(holidays.GetHolidays(year));
}
