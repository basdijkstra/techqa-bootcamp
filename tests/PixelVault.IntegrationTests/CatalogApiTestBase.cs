using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace PixelVault.IntegrationTests;

/// <summary>
/// Base class for tests against the Catalog API. Starts the API in-process on a real Kestrel server
/// at a random free port before the first test in the class, and stops it after the last one.
/// Use <see cref="CatalogBaseUrl"/> to build request URLs, e.g. <c>$"{CatalogBaseUrl}/games/1"</c>.
/// </summary>
public abstract class CatalogApiTestBase
{
    private WebApplicationFactory<Program> factory = null!;
    private string databasePath = null!;

    /// <summary>Where the Catalog API is listening, for example http://127.0.0.1:54321.</summary>
    protected string CatalogBaseUrl { get; private set; } = null!;

    [OneTimeSetUp]
    public void StartCatalogApi()
    {
        // Each test class gets its own SQLite file, so tests never touch the database used by the running app.
        databasePath = Path.Combine(Path.GetTempPath(), $"pixelvault-catalog-{Guid.NewGuid():N}.db");

        factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder
                .UseEnvironment("Development")
                .UseSetting("ConnectionStrings:CatalogDb", $"Data Source={databasePath};Pooling=False"));

        // Port 0 lets the operating system pick a free port.
        factory.UseKestrel(0);
        factory.StartServer();

        CatalogBaseUrl = factory.Services.GetRequiredService<IServer>()
            .Features.GetRequiredFeature<IServerAddressesFeature>()
            .Addresses.First();
    }

    [OneTimeTearDown]
    public void StopCatalogApi()
    {
        factory.Dispose();
        File.Delete(databasePath);
    }
}
