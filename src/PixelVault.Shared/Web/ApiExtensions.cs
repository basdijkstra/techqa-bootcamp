using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using PixelVault.Shared.Auth;
using PixelVault.Shared.Errors;
using PixelVault.Shared.TestSupport;

namespace PixelVault.Shared.Web;

public static class ApiExtensions
{
    private const string CorsPolicy = "PixelVaultFrontend";

    /// <summary>
    /// Registers everything a PixelVault API needs: controllers, problem details, JWT auth, CORS for the
    /// frontend, OpenAPI and <see cref="TimeProvider"/>.
    /// </summary>
    public static WebApplicationBuilder AddPixelVaultApi(this WebApplicationBuilder builder, string serviceTitle)
    {
        // Prices are in US dollars, so numbers and currency are formatted the American way on every machine.
        var culture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        builder.AddServiceDefaults();

        var services = builder.Services;

        services
            .AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                var problem = context.ProblemDetails;
                if (!problem.Extensions.ContainsKey("code"))
                {
                    problem.Extensions["code"] = problem.Status switch
                    {
                        400 => "VALIDATION_FAILED",
                        401 => "UNAUTHORIZED",
                        403 => "FORBIDDEN",
                        404 => "NOT_FOUND",
                        405 => "METHOD_NOT_ALLOWED",
                        _ => "ERROR"
                    };
                }
            };
        });
        services.AddExceptionHandler<DomainExceptionHandler>();

        services.AddPixelVaultAuth(builder.Configuration);

        // The React app calls every service directly from the browser, so each service allows any localhost origin.
        services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
            .SetIsOriginAllowed(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                                          && (uri.IsLoopback || uri.Host == "localhost"))
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Location")));

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = $"PixelVault {serviceTitle} API";
                document.Info.Version = "v1";
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT from POST /auth/login on the Identity service."
                };
                document.Security ??= [];
                document.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
                return Task.CompletedTask;
            });
        });

        services.AddSingleton(TimeProvider.System);

        return builder;
    }

    /// <summary>Configures the request pipeline and maps controllers, health checks, OpenAPI and Swagger UI.</summary>
    public static WebApplication UsePixelVaultApi(this WebApplication app, string serviceTitle)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseCors(CorsPolicy);
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapDefaultEndpoints();
        app.MapTestSupportEndpoints();

        app.MapOpenApi();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", $"PixelVault {serviceTitle} API");
            options.DocumentTitle = $"PixelVault {serviceTitle} API";
        });
        app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

        return app;
    }
}
