using Microsoft.AspNetCore.Mvc;
using PixelVault.Catalog.Domain;
using PixelVault.Catalog.Dtos;
using PixelVault.Catalog.Services;

namespace PixelVault.Catalog.Controllers;

[ApiController]
[Produces("application/json")]
public class CategoriesController(IGameCatalogService catalog) : ControllerBase
{
    /// <summary>All categories with the number of games in each.</summary>
    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(CancellationToken cancellationToken) =>
        Ok(await catalog.GetCategoriesAsync(cancellationToken));

    /// <summary>All platforms.</summary>
    [HttpGet("platforms")]
    public ActionResult<IReadOnlyList<Platform>> GetPlatforms() =>
        Ok(Enum.GetValues<Platform>().Where(p => p != Platform.None).ToList());
}
