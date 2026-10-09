using PixelVault.Identity.Domain;
using PixelVault.Identity.Dtos;
using PixelVault.Identity.Repositories;
using PixelVault.Shared.Errors;

namespace PixelVault.Identity.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<UserDto> GetCurrentUserAsync(string username, CancellationToken cancellationToken = default);
}

/// <summary>
/// Sign-in rules:
/// <list type="bullet">
/// <item>Usernames are case-insensitive and trimmed. Passwords are not trimmed.</item>
/// <item>An unknown username and a wrong password give the same 401 error, so usernames cannot be guessed.</item>
/// <item>After 5 failed attempts in a row the account is locked for 15 minutes (423), even for the right password.
/// The attempt that causes the lockout already returns 423.</item>
/// <item>A successful sign-in resets the failed attempt counter.</item>
/// <item>A disabled account gets 403, but only after the right password was given.</item>
/// </list>
/// </summary>
public class AuthService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenService tokens,
    TimeProvider timeProvider) : IAuthService
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new ValidationErrors();
        errors.AddIf(string.IsNullOrWhiteSpace(request.Username), "username", "Username is required.");
        errors.AddIf(string.IsNullOrEmpty(request.Password), "password", "Password is required.");
        errors.AddIf(request.Username?.Length > 50, "username", "Username cannot be longer than 50 characters.");
        errors.AddIf(request.Password?.Length > 128, "password", "Password cannot be longer than 128 characters.");
        errors.ThrowIfAny();

        var now = timeProvider.GetUtcNow();
        var user = await users.GetByUsernameAsync(request.Username!.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null)
        {
            throw InvalidCredentials();
        }

        if (user.LockoutEndsAt is { } lockoutEndsAt && lockoutEndsAt > now)
        {
            throw Locked(lockoutEndsAt, now);
        }

        if (!passwordHasher.Verify(request.Password!, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.FailedLoginAttempts = 0;
                user.LockoutEndsAt = now.Add(LockoutDuration);
                await users.SaveChangesAsync(cancellationToken);
                throw Locked(user.LockoutEndsAt.Value, now);
            }

            await users.SaveChangesAsync(cancellationToken);
            throw InvalidCredentials();
        }

        if (!user.IsActive)
        {
            throw new DomainStatusException(403, "ACCOUNT_DISABLED", "This account has been disabled. Contact an administrator.");
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEndsAt = null;
        user.LastLoginAt = now;
        await users.SaveChangesAsync(cancellationToken);

        var token = tokens.CreateToken(user);
        return new LoginResponse(token.AccessToken, "Bearer", token.ExpiresAt, ToUserDto(user));
    }

    public async Task<UserDto> GetCurrentUserAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByUsernameAsync(username, cancellationToken) ?? throw new NotFoundException("User", username);
        return ToUserDto(user);
    }

    private static DomainException InvalidCredentials() =>
        new DomainStatusException(401, "INVALID_CREDENTIALS", "The username or password is incorrect.");

    private static DomainException Locked(DateTimeOffset lockoutEndsAt, DateTimeOffset now)
    {
        var exception = new DomainStatusException(423, "ACCOUNT_LOCKED",
            "This account is locked because of too many failed sign-in attempts. Try again later.");
        exception.Details["lockoutEndsAt"] = lockoutEndsAt;
        exception.Details["retryAfterSeconds"] = (int)Math.Ceiling((lockoutEndsAt - now).TotalSeconds);
        return exception;
    }

    private static UserDto ToUserDto(StaffUser user) => new(user.Username, user.DisplayName, user.Role);
}
