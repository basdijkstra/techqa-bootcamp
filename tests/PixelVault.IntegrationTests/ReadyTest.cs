using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

using static RestAssured.Dsl;

namespace PixelVault.IntegrationTests;

/// <summary>
/// Starts the Catalog API in-process on a real Kestrel server at a random free port.
/// </summary>
[TestFixture]
public class ReadyTest
{
    private WebApplicationFactory<Program> factory = null!;
    private string databasePath = null!;
    private string catalogBaseUrl = null!;

    [OneTimeSetUp]
    public void StartCatalogApi()
    {
        // Each run gets its own SQLite file, so tests never touch the database used by the running app.
        databasePath = Path.Combine(Path.GetTempPath(), $"pixelvault-catalog-{Guid.NewGuid():N}.db");

        factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder
                .UseEnvironment("Development")
                .UseSetting("ConnectionStrings:CatalogDb", $"Data Source={databasePath};Pooling=False"));

        // Port 0 lets the operating system pick a free port.
        factory.UseKestrel(0);
        factory.StartServer();

        catalogBaseUrl = factory.Services.GetRequiredService<IServer>()
            .Features.GetRequiredFeature<IServerAddressesFeature>()
            .Addresses.First();
    }

    [Test]
    public void AreYouReady()
    {
        // catalogBaseUrl (for example http://127.0.0.1:54321) is where the Catalog API is listening.
        Given()
            .When()
            .Get($"{catalogBaseUrl}/health")
            .Then()
            .StatusCode(200);
    }    

    [OneTimeTearDown]
    public void StopCatalogApi()
    {
        factory.Dispose();
        File.Delete(databasePath);
    }
}
