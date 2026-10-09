using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PixelVault.Identity.Domain;
using PixelVault.Identity.Services;
using PixelVault.Shared.Auth;
using PixelVault.Shared.TestSupport;

namespace PixelVault.Identity.Infrastructure;

public class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<StaffUser> Users => Set<StaffUser>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite cannot sort or compare DateTimeOffset values, storing them as numbers fixes that.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StaffUser>().HasIndex(u => u.Username).IsUnique();
    }
}

/// <summary>
/// Seeds three staff accounts:
/// <code>
/// admin            Admin123!     Admin (full access)
/// support          Support123!   Support (read-only)
/// former.employee  Former123!    disabled account
/// </code>
/// </summary>
public class IdentitySeeder(IdentityDbContext db, IPasswordHasher passwordHasher) : IDataSeeder
{
    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Database.EnsureCreatedAsync(cancellationToken))
        {
            await SeedAsync(cancellationToken);
        }
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureDeletedAsync(cancellationToken);
        await db.Database.EnsureCreatedAsync(cancellationToken);
        await SeedAsync(cancellationToken);
    }

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        db.Users.AddRange(
            new StaffUser { Username = "admin", DisplayName = "Alex Admin", Role = Roles.Admin, PasswordHash = passwordHasher.Hash("Admin123!") },
            new StaffUser { Username = "support", DisplayName = "Sam Support", Role = Roles.Support, PasswordHash = passwordHasher.Hash("Support123!") },
            new StaffUser { Username = "former.employee", DisplayName = "Former Employee", Role = Roles.Admin, PasswordHash = passwordHasher.Hash("Former123!"), IsActive = false });

        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }
}
