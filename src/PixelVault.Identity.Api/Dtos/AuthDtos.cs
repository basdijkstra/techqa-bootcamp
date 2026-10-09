namespace PixelVault.Identity.Dtos;

public record LoginRequest(string? Username, string? Password);

public record UserDto(string Username, string DisplayName, string Role);

public record LoginResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAt, UserDto User);

public record StaffUserDto(
    string Username,
    string DisplayName,
    string Role,
    bool IsActive,
    bool IsLockedOut,
    DateTimeOffset? LockoutEndsAt,
    int FailedLoginAttempts,
    DateTimeOffset? LastLoginAt);
