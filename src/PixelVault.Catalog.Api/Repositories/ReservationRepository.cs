using Microsoft.EntityFrameworkCore;
using PixelVault.Catalog.Domain;
using PixelVault.Catalog.Infrastructure;

namespace PixelVault.Catalog.Repositories;

public interface IReservationRepository
{
    Task<IReadOnlyList<StockReservation>> GetByOrderReferenceAsync(string orderReference, CancellationToken cancellationToken = default);

    /// <summary>Adds the reservations. They are saved together with the games by <see cref="IGameRepository.SaveChangesAsync"/>.</summary>
    void AddRange(IEnumerable<StockReservation> reservations);
}

public class ReservationRepository(CatalogDbContext db) : IReservationRepository
{
    public async Task<IReadOnlyList<StockReservation>> GetByOrderReferenceAsync(string orderReference, CancellationToken cancellationToken = default) =>
        await db.StockReservations
            .Where(r => r.OrderReference == orderReference)
            .ToListAsync(cancellationToken);

    public void AddRange(IEnumerable<StockReservation> reservations) => db.StockReservations.AddRange(reservations);
}
