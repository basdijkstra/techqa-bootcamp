using PixelVault.Identity.Dtos;
using PixelVault.Identity.Repositories;
using PixelVault.Shared.Errors;

namespace PixelVault.Identity.Services;

public interface IUserService
{
    Task<IReadOnlyList<StaffUserDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Ends a lockout and resets the failed attempt counter.</summary>
    Task<StaffUserDto> UnlockAsync(string username, CancellationToken cancellationToken = default);
}

public class UserService(IUserRepository users, TimeProvider timeProvider) : IUserService
{
    public async Task<IReadOnlyList<StaffUserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var all = await users.GetAllAsync(cancellationToken);
        return all.OrderBy(u => u.Username).Select(u => ToDto(u, now)).ToList();
    }

    public async Task<StaffUserDto> UnlockAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByUsernameAsync(username.Trim().ToLowerInvariant(), cancellationToken)
                   ?? throw new NotFoundException("User", username);

        user.LockoutEndsAt = null;
        user.FailedLoginAttempts = 0;
        await users.SaveChangesAsync(cancellationToken);
        return ToDto(user, timeProvider.GetUtcNow());
    }

    private static StaffUserDto ToDto(Domain.StaffUser u, DateTimeOffset now) => new(
        u.Username,
        u.DisplayName,
        u.Role,
        u.IsActive,
        u.LockoutEndsAt > now,
        u.LockoutEndsAt > now ? u.LockoutEndsAt : null,
        u.FailedLoginAttempts,
        u.LastLoginAt);
}
