namespace PixelVault.Shared.Auth;

/// <summary>
/// JWT settings shared by the Identity service (which issues tokens) and every service that validates them.
/// Bound from the <c>Jwt</c> configuration section; the defaults are for local development only.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "pixelvault-identity";

    public string Audience { get; set; } = "pixelvault";

    public string SigningKey { get; set; } = "pixelvault-local-development-signing-key-change-me-0123456789";

    public int TokenLifetimeMinutes { get; set; } = 60;
}
