namespace PixelVault.Shared.Auth;

public static class Roles
{
    /// <summary>Full access: stock changes, order status changes.</summary>
    public const string Admin = "Admin";

    /// <summary>Read-only access to orders and stock.</summary>
    public const string Support = "Support";
}

public static class Policies
{
    /// <summary>Requires the Admin role.</summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>Requires the Admin or Support role.</summary>
    public const string Staff = "Staff";
}
