using Microsoft.EntityFrameworkCore;
using PixelVault.Identity.Domain;
using PixelVault.Identity.Infrastructure;

namespace PixelVault.Identity.Repositories;

public interface IUserRepository
{
    /// <summary>Gets a user by lower-case username, or null.</summary>
    Task<StaffUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaffUser>> GetAllAsync(CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class UserRepository(IdentityDbContext db) : IUserRepository
{
    public Task<StaffUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

    public async Task<IReadOnlyList<StaffUser>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Users.ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
}
