namespace PixelVault.IntegrationTests.Models;

/// <summary>
/// A game as returned by GET /games/{id}. Only contains the properties the tests use:
/// properties in the JSON response that are not in this class are ignored when deserializing.
/// </summary>
public class Game
{
    public int Id { get; set; }

    public string Slug { get; set; } = "";

    public string Title { get; set; } = "";

    public string Publisher { get; set; } = "";

    public string Category { get; set; } = "";

    public List<string> Platforms { get; set; } = [];

    public string AgeRating { get; set; } = "";

    public int MinimumAge { get; set; }

    public Pricing Pricing { get; set; } = new();

    public string StockStatus { get; set; } = "";

    public int AvailableQuantity { get; set; }
}

public class Pricing
{
    public decimal BasePrice { get; set; }

    public decimal Price { get; set; }

    public bool IsOnSale { get; set; }
}
