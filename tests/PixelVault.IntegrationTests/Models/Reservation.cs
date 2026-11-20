using Newtonsoft.Json;

namespace PixelVault.IntegrationTests.Models;

/// <summary>
/// The request body for POST /reservations.
/// RestAssured.Net serializes objects with Newtonsoft.Json, which uses the C# property names (OrderReference)
/// by default. The [JsonProperty] attributes make the JSON use the same camelCase names as the API (orderReference).
/// </summary>
public class ReservationRequest
{
    [JsonProperty("orderReference")]
    public string OrderReference { get; set; } = "";

    [JsonProperty("items")]
    public List<ReservationRequestItem> Items { get; set; } = [];
}

public class ReservationRequestItem
{
    [JsonProperty("gameId")]
    public int GameId { get; set; }

    [JsonProperty("quantity")]
    public int Quantity { get; set; }
}

/// <summary>
/// A reservation as returned by POST /reservations and GET /reservations/{orderReference}.
/// No [JsonProperty] attributes needed here: when deserializing, Newtonsoft.Json matches names case-insensitively.
/// </summary>
public class Reservation
{
    public string OrderReference { get; set; } = "";

    public string Status { get; set; } = "";

    public List<ReservationItem> Items { get; set; } = [];
}

public class ReservationItem
{
    public int GameId { get; set; }

    public int Quantity { get; set; }

    /// <summary>False for pre-orders: they are reserved, but don't take units out of stock.</summary>
    public bool ConsumesStock { get; set; }
}
