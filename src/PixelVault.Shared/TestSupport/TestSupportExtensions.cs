using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PixelVault.Shared.TestSupport;

public static class TestSupportExtensions
{
    /// <summary>
    /// Creates and seeds the database at startup. With <c>Database:ResetOnStartup</c> set to true (the default)
    /// every start begins from the original seed data.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Database");

        if (app.Configuration.GetValue("Database:ResetOnStartup", true))
        {
            logger.LogInformation("Resetting database to seed data");
            await seeder.ResetAsync();
        }
        else
        {
            await seeder.EnsureSeededAsync();
        }
    }

    /// <summary>
    /// Maps <c>POST /test-support/reset</c>, which restores the seed data. Only mapped when
    /// <c>TestSupport:Enabled</c> is true (it is in the Development environment).
    /// </summary>
    public static WebApplication MapTestSupportEndpoints(this WebApplication app)
    {
        if (!app.Configuration.GetValue("TestSupport:Enabled", false))
        {
            return app;
        }

        app.MapPost("/test-support/reset", async (IDataSeeder seeder, CancellationToken cancellationToken) =>
            {
                await seeder.ResetAsync(cancellationToken);
                return Results.NoContent();
            })
            .WithTags("Test support")
            .WithSummary("Deletes all data and restores the original seed data.");

        return app;
    }
}
