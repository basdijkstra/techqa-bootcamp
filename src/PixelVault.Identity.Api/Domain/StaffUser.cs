namespace PixelVault.Identity.Domain;

/// <summary>A PixelVault staff member who can sign in to the admin pages. Customers don't have accounts.</summary>
public class StaffUser
{
    public int Id { get; set; }

    /// <summary>Stored in lower case. Sign-in is case-insensitive.</summary>
    public string Username { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    /// <summary>Admin or Support, see <see cref="PixelVault.Shared.Auth.Roles"/>.</summary>
    public string Role { get; set; } = "";

    public bool IsActive { get; set; } = true;

    /// <summary>Failed attempts since the last successful sign-in or lockout.</summary>
    public int FailedLoginAttempts { get; set; }

    public DateTimeOffset? LockoutEndsAt { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }
}
