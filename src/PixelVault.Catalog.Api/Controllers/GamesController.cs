using Microsoft.AspNetCore.Mvc;
using PixelVault.Catalog.Dtos;
using PixelVault.Catalog.Services;

namespace PixelVault.Catalog.Controllers;

[ApiController]
[Route("games")]
[Produces("application/json")]
public class GamesController(IGameCatalogService catalog) : ControllerBase
{
    /// <summary>Searches the catalog with filters, sorting and paging.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<GameSummaryDto>>> Search([FromQuery] GameSearchQuery query, CancellationToken cancellationToken) =>
        Ok(await catalog.SearchAsync(query, cancellationToken));

    /// <summary>Gets several games at once, e.g. <c>?ids=1&amp;ids=2</c>. Unknown ids are left out of the result.</summary>
    [HttpGet("batch")]
    public async Task<ActionResult<IReadOnlyList<GameSummaryDto>>> GetMany([FromQuery] int[] ids, CancellationToken cancellationToken) =>
        Ok(await catalog.GetManyAsync(ids, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameDetailDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await catalog.GetByIdAsync(id, cancellationToken));

    [HttpGet("by-slug/{slug}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameDetailDto>> GetBySlug(string slug, CancellationToken cancellationToken) =>
        Ok(await catalog.GetBySlugAsync(slug, cancellationToken));
}
