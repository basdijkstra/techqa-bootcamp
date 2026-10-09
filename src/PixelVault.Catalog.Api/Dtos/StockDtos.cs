using PixelVault.Catalog.Domain;

namespace PixelVault.Catalog.Dtos;

public record StockItemDto(
    int GameId,
    string Title,
    Category Category,
    int StockQuantity,
    StockStatus StockStatus,
    DateOnly ReleaseDate,
    decimal Price);

public record AdjustStockRequest(int QuantityChange, string? Reason, string? Note);

public record StockAdjustmentDto(
    int Id,
    int GameId,
    int QuantityChange,
    int QuantityBefore,
    int QuantityAfter,
    StockAdjustmentReason Reason,
    string? Note,
    string PerformedBy,
    DateTimeOffset PerformedAt);

public record ReservationItemRequest(int GameId, int Quantity);

public record CreateReservationRequest(string? OrderReference, IReadOnlyList<ReservationItemRequest>? Items);

public record ReservationItemDto(int GameId, int Quantity, bool ConsumesStock);

public record ReservationDto(string OrderReference, ReservationStatus Status, IReadOnlyList<ReservationItemDto> Items);

public record StockShortageDto(int GameId, string Title, int Requested, int Available);
