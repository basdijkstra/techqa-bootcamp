using PixelVault.Catalog.Domain;
using PixelVault.Catalog.Dtos;
using PixelVault.Catalog.Repositories;
using PixelVault.Shared.Errors;

namespace PixelVault.Catalog.Services;

public interface IReservationService
{
    /// <summary>Reserves stock for an order. Returns whether a new reservation was created.</summary>
    Task<(ReservationDto Reservation, bool Created)> ReserveAsync(CreateReservationRequest request, CancellationToken cancellationToken = default);

    Task<ReservationDto> GetAsync(string orderReference, CancellationToken cancellationToken = default);

    Task<ReservationDto> CommitAsync(string orderReference, CancellationToken cancellationToken = default);

    Task<ReservationDto> ReleaseAsync(string orderReference, CancellationToken cancellationToken = default);
}

/// <summary>
/// Holds stock for an order during checkout. Called by the Order service, not by the browser.
/// <list type="bullet">
/// <item>Reserving is all or nothing: if one game is short, nothing is reserved and every shortage is reported.</item>
/// <item>Lines for the same game are combined.</item>
/// <item>Pre-orders are reserved, but don't take units out of stock.</item>
/// <item>Reserving twice for the same order returns the existing active reservation (idempotent).</item>
/// <item>Commit marks a reservation as final once payment succeeded. Committing twice is fine.</item>
/// <item>Release puts the units back in stock (payment failed, or order cancelled). Releasing twice is fine.</item>
/// <item>A released reservation cannot be committed, and a finished reservation cannot be reserved again.</item>
/// </list>
/// </summary>
public class ReservationService(
    IGameRepository games,
    IReservationRepository reservations,
    IStockStatusResolver stockStatus,
    TimeProvider timeProvider) : IReservationService
{
    public async Task<(ReservationDto Reservation, bool Created)> ReserveAsync(CreateReservationRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new ValidationErrors();
        errors.AddIf(string.IsNullOrWhiteSpace(request.OrderReference), "orderReference", "Order reference is required.");
        errors.AddIf(request.Items is null || request.Items.Count == 0, "items", "At least one item is required.");
        errors.AddIf(request.Items?.Any(i => i.Quantity <= 0) == true, "items", "Every quantity must be at least 1.");
        errors.ThrowIfAny();

        var orderReference = request.OrderReference!.Trim();
        var existing = await reservations.GetByOrderReferenceAsync(orderReference, cancellationToken);
        if (existing.Count > 0)
        {
            if (existing.All(r => r.Status == ReservationStatus.Active))
            {
                return (ToDto(orderReference, existing), false);
            }

            throw new ConflictException(
                "RESERVATION_ALREADY_FINALIZED",
                $"The reservation for order '{orderReference}' has already been committed or released.");
        }

        var lines = request.Items!
            .GroupBy(i => i.GameId)
            .Select(g => (GameId: g.Key, Quantity: g.Sum(i => i.Quantity)))
            .ToList();

        var found = (await games.GetByIdsAsync(lines.Select(l => l.GameId), cancellationToken)).ToDictionary(g => g.Id);
        var missing = lines.FirstOrDefault(l => !found.ContainsKey(l.GameId));
        if (missing != default)
        {
            throw new NotFoundException("Game", missing.GameId);
        }

        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var shortages = new List<StockShortageDto>();
        var newReservations = new List<StockReservation>();

        foreach (var (gameId, quantity) in lines)
        {
            var game = found[gameId];
            var consumesStock = stockStatus.Resolve(game, today) != StockStatus.PreOrder;

            if (consumesStock && quantity > game.StockQuantity)
            {
                shortages.Add(new StockShortageDto(game.Id, game.Title, quantity, Math.Max(game.StockQuantity, 0)));
                continue;
            }

            newReservations.Add(new StockReservation
            {
                OrderReference = orderReference,
                GameId = gameId,
                Quantity = quantity,
                ConsumesStock = consumesStock,
                Status = ReservationStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (shortages.Count > 0)
        {
            var exception = new ConflictException("INSUFFICIENT_STOCK", "Not enough stock for one or more games.");
            exception.Details["items"] = shortages;
            throw exception;
        }

        foreach (var reservation in newReservations.Where(r => r.ConsumesStock))
        {
            found[reservation.GameId].StockQuantity -= reservation.Quantity;
        }

        reservations.AddRange(newReservations);
        await games.SaveChangesAsync(cancellationToken);

        return (ToDto(orderReference, newReservations), true);
    }

    public async Task<ReservationDto> GetAsync(string orderReference, CancellationToken cancellationToken = default)
    {
        var existing = await GetExistingAsync(orderReference, cancellationToken);
        return ToDto(orderReference, existing);
    }

    public async Task<ReservationDto> CommitAsync(string orderReference, CancellationToken cancellationToken = default)
    {
        var existing = await GetExistingAsync(orderReference, cancellationToken);

        if (existing.Any(r => r.Status == ReservationStatus.Released))
        {
            throw new ConflictException(
                "RESERVATION_RELEASED",
                $"The reservation for order '{orderReference}' was released and cannot be committed.");
        }

        var now = timeProvider.GetUtcNow();
        foreach (var reservation in existing.Where(r => r.Status == ReservationStatus.Active))
        {
            reservation.Status = ReservationStatus.Committed;
            reservation.UpdatedAt = now;
        }

        await games.SaveChangesAsync(cancellationToken);
        return ToDto(orderReference, existing);
    }

    public async Task<ReservationDto> ReleaseAsync(string orderReference, CancellationToken cancellationToken = default)
    {
        var existing = await GetExistingAsync(orderReference, cancellationToken);
        var toRelease = existing.Where(r => r.Status != ReservationStatus.Released).ToList();
        if (toRelease.Count == 0)
        {
            return ToDto(orderReference, existing);
        }

        var gamesById = (await games.GetByIdsAsync(toRelease.Select(r => r.GameId), cancellationToken)).ToDictionary(g => g.Id);
        var now = timeProvider.GetUtcNow();

        foreach (var reservation in toRelease)
        {
            if (reservation.ConsumesStock && gamesById.TryGetValue(reservation.GameId, out var game))
            {
                game.StockQuantity += reservation.Quantity;
            }

            reservation.Status = ReservationStatus.Released;
            reservation.UpdatedAt = now;
        }

        await games.SaveChangesAsync(cancellationToken);
        return ToDto(orderReference, existing);
    }

    private async Task<IReadOnlyList<StockReservation>> GetExistingAsync(string orderReference, CancellationToken cancellationToken)
    {
        var existing = await reservations.GetByOrderReferenceAsync(orderReference.Trim(), cancellationToken);
        return existing.Count == 0 ? throw new NotFoundException("Reservation", orderReference) : existing;
    }

    private static ReservationDto ToDto(string orderReference, IReadOnlyList<StockReservation> reservations)
    {
        var status = reservations.All(r => r.Status == ReservationStatus.Committed) ? ReservationStatus.Committed
            : reservations.All(r => r.Status == ReservationStatus.Released) ? ReservationStatus.Released
            : ReservationStatus.Active;

        return new ReservationDto(
            orderReference,
            status,
            reservations.Select(r => new ReservationItemDto(r.GameId, r.Quantity, r.ConsumesStock)).ToList());
    }
}
