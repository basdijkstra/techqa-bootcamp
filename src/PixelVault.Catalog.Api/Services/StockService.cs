using PixelVault.Catalog.Domain;
using PixelVault.Catalog.Dtos;
using PixelVault.Catalog.Repositories;
using PixelVault.Shared.Errors;

namespace PixelVault.Catalog.Services;

public interface IStockService
{
    Task<IReadOnlyList<StockItemDto>> GetStockOverviewAsync(CancellationToken cancellationToken = default);

    Task<StockAdjustmentDto> AdjustStockAsync(int gameId, AdjustStockRequest request, string performedBy, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockAdjustmentDto>> GetAdjustmentHistoryAsync(int gameId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Manual stock changes by staff. Rules:
/// <list type="bullet">
/// <item>The change cannot be zero and is at most 1000 units in either direction.</item>
/// <item>Restock and Return must add stock, Damaged and Lost must remove stock.</item>
/// <item>Correction can go either way, but needs a note.</item>
/// <item>Notes are at most 200 characters.</item>
/// <item>Stock can never go below 0 or above 9999.</item>
/// <item>Every change is written to the audit log.</item>
/// </list>
/// </summary>
public class StockService(
    IGameRepository games,
    IStockAdjustmentRepository adjustments,
    IPricingCalculator pricing,
    IStockStatusResolver stockStatus,
    TimeProvider timeProvider) : IStockService
{
    public const int MaxQuantityChange = 1000;
    public const int MaxStockQuantity = 9999;
    public const int MaxNoteLength = 200;

    public async Task<IReadOnlyList<StockItemDto>> GetStockOverviewAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var all = await games.GetAllAsync(cancellationToken);

        return all
            .OrderBy(g => g.Title)
            .Select(g => new StockItemDto(
                g.Id,
                g.Title,
                g.Category,
                g.StockQuantity,
                stockStatus.Resolve(g, today),
                g.ReleaseDate,
                pricing.Calculate(g, now).Price))
            .ToList();
    }

    public async Task<StockAdjustmentDto> AdjustStockAsync(int gameId, AdjustStockRequest request, string performedBy, CancellationToken cancellationToken = default)
    {
        var reason = ValidateRequest(request);

        var game = await games.GetByIdAsync(gameId, cancellationToken) ?? throw new NotFoundException("Game", gameId);

        var before = game.StockQuantity;
        var after = before + request.QuantityChange;

        if (after < 0)
        {
            throw new BusinessRuleException(
                "NEGATIVE_STOCK",
                $"Cannot remove {-request.QuantityChange} units from '{game.Title}': only {before} in stock.");
        }

        if (after > MaxStockQuantity)
        {
            throw new BusinessRuleException(
                "STOCK_LIMIT_EXCEEDED",
                $"Stock for '{game.Title}' cannot exceed {MaxStockQuantity} units.");
        }

        game.StockQuantity = after;
        var adjustment = new StockAdjustment
        {
            GameId = game.Id,
            QuantityChange = request.QuantityChange,
            QuantityBefore = before,
            QuantityAfter = after,
            Reason = reason,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            PerformedBy = performedBy,
            PerformedAt = timeProvider.GetUtcNow()
        };
        adjustments.Add(adjustment);
        await games.SaveChangesAsync(cancellationToken);

        return ToDto(adjustment);
    }

    public async Task<IReadOnlyList<StockAdjustmentDto>> GetAdjustmentHistoryAsync(int gameId, CancellationToken cancellationToken = default)
    {
        _ = await games.GetByIdAsync(gameId, cancellationToken) ?? throw new NotFoundException("Game", gameId);
        var history = await adjustments.GetForGameAsync(gameId, cancellationToken);
        return history.Select(ToDto).ToList();
    }

    private static StockAdjustmentReason ValidateRequest(AdjustStockRequest request)
    {
        var errors = new ValidationErrors();

        if (request.QuantityChange == 0)
        {
            errors.Add("quantityChange", "Quantity change cannot be zero.");
        }
        else if (Math.Abs(request.QuantityChange) > MaxQuantityChange)
        {
            errors.Add("quantityChange", $"Quantity change cannot be more than {MaxQuantityChange} units at once.");
        }

        errors.AddIf(request.Note?.Length > MaxNoteLength, "note", $"Note cannot be longer than {MaxNoteLength} characters.");

        if (!Enum.TryParse<StockAdjustmentReason>(request.Reason, ignoreCase: true, out var reason) || !Enum.IsDefined(reason))
        {
            errors.Add("reason", $"Reason must be one of: {string.Join(", ", Enum.GetNames<StockAdjustmentReason>())}.");
            errors.ThrowIfAny();
        }

        switch (reason)
        {
            case StockAdjustmentReason.Restock or StockAdjustmentReason.Return when request.QuantityChange < 0:
                errors.Add("quantityChange", $"{reason} must add stock (positive quantity change).");
                break;
            case StockAdjustmentReason.Damaged or StockAdjustmentReason.Lost when request.QuantityChange > 0:
                errors.Add("quantityChange", $"{reason} must remove stock (negative quantity change).");
                break;
            case StockAdjustmentReason.Correction when string.IsNullOrWhiteSpace(request.Note):
                errors.Add("note", "A note is required for a correction.");
                break;
        }

        errors.ThrowIfAny();
        return reason;
    }

    private static StockAdjustmentDto ToDto(StockAdjustment a) => new(
        a.Id, a.GameId, a.QuantityChange, a.QuantityBefore, a.QuantityAfter, a.Reason, a.Note, a.PerformedBy, a.PerformedAt);
}
