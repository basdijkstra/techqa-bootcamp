using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PixelVault.Identity.Domain;
using PixelVault.Shared.Auth;

namespace PixelVault.Identity.Services;

public record IssuedToken(string AccessToken, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    IssuedToken CreateToken(StaffUser user);
}

/// <summary>Issues signed JWTs that every PixelVault service accepts. Lifetime comes from <see cref="JwtSettings"/>.</summary>
public class JwtTokenService(JwtSettings settings, TimeProvider timeProvider) : ITokenService
{
    public IssuedToken CreateToken(StaffUser user)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(settings.TokenLifetimeMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", user.Username),
                new Claim("name", user.Username),
                new Claim("display_name", user.DisplayName),
                new Claim("role", user.Role)
            ])
        };

        return new IssuedToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }
}
