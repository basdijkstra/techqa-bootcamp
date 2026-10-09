using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace PixelVault.Shared.Auth;

public static class AuthExtensions
{
    public static IServiceCollection AddPixelVaultAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
        services.AddSingleton(settings);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(Roles.Admin))
            .AddPolicy(Policies.Staff, policy => policy.RequireRole(Roles.Admin, Roles.Support));

        return services;
    }

    /// <summary>The username of the signed in staff member, or "system" when there is none.</summary>
    public static string GetUsername(this ClaimsPrincipal user) =>
        user.FindFirstValue("name") ?? user.FindFirstValue("sub") ?? "system";
}
