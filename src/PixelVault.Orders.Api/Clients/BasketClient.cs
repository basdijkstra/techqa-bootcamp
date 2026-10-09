using PixelVault.Shared.Errors;

namespace PixelVault.Orders.Clients;

public record BasketLineSnapshot(
    int GameId,
    string Title,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    bool IsPreOrder,
    DateOnly? ReleaseDate,
    IReadOnlyList<string> Issues);

/// <summary>The parts of a Basket that the Order service needs.</summary>
public record BasketSnapshot(
    Guid Id,
    string Status,
    IReadOnlyList<BasketLineSnapshot> Items,
    int ItemCount,
    decimal Subtotal,
    decimal DiscountTotal,
    string? DiscountDescription,
    decimal Total,
    int MinimumAge,
    DateOnly? LatestReleaseDate,
    bool HasIssues,
    bool CanCheckout);

public interface IBasketClient
{
    /// <summary>Gets a basket. Throws with the Basket service's error (404, 410) when it can't be used.</summary>
    Task<BasketSnapshot> GetBasketAsync(Guid basketId, CancellationToken cancellationToken = default);

    /// <summary>Marks the basket as checked out for an order.</summary>
    Task CheckoutBasketAsync(Guid basketId, string orderReference, CancellationToken cancellationToken = default);
}

public class BasketClient(HttpClient httpClient) : IBasketClient
{
    private const string ServiceName = "Basket";

    public async Task<BasketSnapshot> GetBasketAsync(Guid basketId, CancellationToken cancellationToken = default)
    {
        using var response = await DownstreamCall.SendAsync(ServiceName, () => httpClient.GetAsync($"baskets/{basketId}", cancellationToken));
        await response.EnsureSuccessOrThrowAsync(ServiceName, cancellationToken);
        return await response.Content.ReadFromJsonAsync<BasketSnapshot>(cancellationToken)
               ?? throw new DownstreamServiceException(ServiceName, "empty response");
    }

    public async Task CheckoutBasketAsync(Guid basketId, string orderReference, CancellationToken cancellationToken = default)
    {
        using var response = await DownstreamCall.SendAsync(ServiceName, () =>
            httpClient.PostAsJsonAsync($"baskets/{basketId}/checkout", new { orderReference }, cancellationToken));
        await response.EnsureSuccessOrThrowAsync(ServiceName, cancellationToken);
    }
}
