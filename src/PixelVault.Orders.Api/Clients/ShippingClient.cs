using System.Net;
using PixelVault.Shared.Errors;

namespace PixelVault.Orders.Clients;

public record ShippingAddressSnapshot(string Line1, string? Line2, string City, string Region, string PostalCode, string Country);

public record ShippingOptionSnapshot(string Method, decimal Cost, bool IsFree, DateOnly ShipDate, DateOnly EstimatedDeliveryDate);

public record ShippingQuoteSnapshot(
    Guid Id,
    DateTimeOffset ExpiresAt,
    bool IsExpired,
    ShippingAddressSnapshot Address,
    string Zone,
    int ItemCount,
    decimal OrderSubtotal,
    IReadOnlyList<ShippingOptionSnapshot> Options);

public interface IShippingClient
{
    /// <summary>Gets a shipping quote, or null when it doesn't exist.</summary>
    Task<ShippingQuoteSnapshot?> GetQuoteAsync(Guid quoteId, CancellationToken cancellationToken = default);
}

public class ShippingClient(HttpClient httpClient) : IShippingClient
{
    private const string ServiceName = "Shipping";

    public async Task<ShippingQuoteSnapshot?> GetQuoteAsync(Guid quoteId, CancellationToken cancellationToken = default)
    {
        using var response = await DownstreamCall.SendAsync(ServiceName, () => httpClient.GetAsync($"quotes/{quoteId}", cancellationToken));
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await response.EnsureSuccessOrThrowAsync(ServiceName, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ShippingQuoteSnapshot>(cancellationToken);
    }
}
