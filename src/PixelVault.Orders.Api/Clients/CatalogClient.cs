using PixelVault.Shared.Errors;

namespace PixelVault.Orders.Clients;

public record ReservationItem(int GameId, int Quantity);

public interface ICatalogClient
{
    /// <summary>Reserves stock. Throws a 409 INSUFFICIENT_STOCK error when there is not enough.</summary>
    Task ReserveStockAsync(string orderReference, IEnumerable<ReservationItem> items, CancellationToken cancellationToken = default);

    Task CommitReservationAsync(string orderReference, CancellationToken cancellationToken = default);

    /// <summary>Puts reserved (or committed) stock back.</summary>
    Task ReleaseReservationAsync(string orderReference, CancellationToken cancellationToken = default);
}

public class CatalogClient(HttpClient httpClient) : ICatalogClient
{
    private const string ServiceName = "Catalog";

    public async Task ReserveStockAsync(string orderReference, IEnumerable<ReservationItem> items, CancellationToken cancellationToken = default)
    {
        using var response = await DownstreamCall.SendAsync(ServiceName, () =>
            httpClient.PostAsJsonAsync("reservations", new { orderReference, items }, cancellationToken));
        await response.EnsureSuccessOrThrowAsync(ServiceName, cancellationToken);
    }

    public async Task CommitReservationAsync(string orderReference, CancellationToken cancellationToken = default)
    {
        using var response = await DownstreamCall.SendAsync(ServiceName, () =>
            httpClient.PostAsync($"reservations/{Uri.EscapeDataString(orderReference)}/commit", null, cancellationToken));
        await response.EnsureSuccessOrThrowAsync(ServiceName, cancellationToken);
    }

    public async Task ReleaseReservationAsync(string orderReference, CancellationToken cancellationToken = default)
    {
        using var response = await DownstreamCall.SendAsync(ServiceName, () =>
            httpClient.PostAsync($"reservations/{Uri.EscapeDataString(orderReference)}/release", null, cancellationToken));
        await response.EnsureSuccessOrThrowAsync(ServiceName, cancellationToken);
    }
}
