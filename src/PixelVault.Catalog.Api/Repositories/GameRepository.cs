using Microsoft.EntityFrameworkCore;
using PixelVault.Catalog.Domain;
using PixelVault.Catalog.Infrastructure;

namespace PixelVault.Catalog.Repositories;

public interface IGameRepository
{
    Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Game?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Game?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Game>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class GameRepository(CatalogDbContext db) : IGameRepository
{
    public async Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Games.ToListAsync(cancellationToken);

    public Task<Game?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.Games.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public Task<Game?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        db.Games.FirstOrDefaultAsync(g => g.Slug == slug, cancellationToken);

    public async Task<IReadOnlyList<Game>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        return await db.Games.Where(g => idList.Contains(g.Id)).ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
