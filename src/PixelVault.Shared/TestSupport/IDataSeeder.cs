namespace PixelVault.Shared.TestSupport;

/// <summary>
/// Implemented by each service to create its SQLite database and fill it with seed data.
/// </summary>
public interface IDataSeeder
{
    /// <summary>Creates the database and seeds it, but only when it does not exist yet.</summary>
    Task EnsureSeededAsync(CancellationToken cancellationToken = default);

    /// <summary>Deletes all data and restores the original seed data.</summary>
    Task ResetAsync(CancellationToken cancellationToken = default);
}
