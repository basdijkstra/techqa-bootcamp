using PixelVault.Shared.Web;

namespace PixelVault.Shared.Seed;

public record SeedOrderLine(int GameId, string Title, decimal UnitPrice, int Quantity, bool IsPreOrder = false)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

public record SeedCustomer(string FullName, string Email, string Phone);

public record SeedAddress(string Line1, string City, string Region, string PostalCode, string Country);

/// <summary>
/// One historical order. Order and Payment both seed from this list, so that every seeded order has a
/// matching payment that can be refunded.
/// </summary>
public record SeedOrder(
    int Index,
    string Suffix,
    double DaysAgo,
    string Status,
    SeedCustomer Customer,
    SeedAddress Address,
    IReadOnlyList<SeedOrderLine> Lines,
    string ShippingMethod,
    decimal ShippingCost,
    decimal Discount,
    string? DiscountDescription,
    decimal TaxRate,
    bool ShippingTaxable,
    string CardBrand,
    string CardLast4,
    double? ShippedDaysAgo = null,
    double? DeliveredDaysAgo = null,
    double? ClosedDaysAgo = null,
    string? DeclineCode = null)
{
    public Guid PaymentId => new($"00000000-0000-0000-0000-{Index:D12}");

    public decimal Subtotal => Lines.Sum(l => l.LineTotal);

    public decimal Tax => MoneyMath.RoundToCents((Subtotal - Discount + (ShippingTaxable ? ShippingCost : 0m)) * TaxRate);

    public decimal Total => Subtotal - Discount + ShippingCost + Tax;

    public DateTimeOffset CreatedAt(DateTimeOffset now) => now.AddDays(-DaysAgo);

    public string OrderNumber(DateTimeOffset now) => $"PV-{CreatedAt(now):yyyyMMdd}-{Suffix}";

    public string? TrackingNumber => ShippedDaysAgo is null ? null : $"1Z{Index:D2}PV{Suffix}0{Index * 7919 % 100000:D5}";
}

/// <summary>
/// 15 historical orders in every status, at points in time that matter for the business rules:
/// paid less than and more than 24 hours ago (customer cancellation), delivered less than and more than
/// 30 days ago (refunds), a pre-order that cannot ship yet, Canada, Hawaii, discounts, and a declined payment.
/// </summary>
public static class SeedOrders
{
    public static readonly IReadOnlyList<SeedOrder> All =
    [
        new(1, "K7M2Q", 45, "Delivered",
            new("Emma Johnson", "emma.johnson@example.com", "2175550142"),
            new("1200 Maple Avenue", "Springfield", "IL", "62704", "US"),
            [new(1, "Ashes of Valdren", 59.99m, 1)],
            "Standard", 0m, 0m, null, 0.0625m, false, "Visa", "4242",
            ShippedDaysAgo: 44, DeliveredDaysAgo: 38),

        new(2, "R4T8W", 20, "Delivered",
            new("Michael Chen", "michael.chen@example.com", "2065550199"),
            new("88 Harbor Street", "Seattle", "WA", "98101", "US"),
            [new(7, "Goalrush 26", 69.99m, 1), new(19, "Court Legends Basketball", 54.99m, 1)],
            "Express", 14.99m, 0m, null, 0.065m, true, "Mastercard", "4444",
            ShippedDaysAgo: 19, DeliveredDaysAgo: 14),

        new(3, "H3N9P", 12, "Refunded",
            new("Sofia Martinez", "sofia.martinez@example.com", "3055550123"),
            new("455 Ocean Drive", "Miami", "FL", "33139", "US"),
            [new(3, "Hollow Lantern: Uncut", 39.99m, 1)],
            "Standard", 5.99m, 0m, null, 0.06m, false, "Visa", "4242",
            ShippedDaysAgo: 11, DeliveredDaysAgo: 6, ClosedDaysAgo: 2),

        new(4, "B6V2X", 8, "Shipped",
            new("James Wilson", "james.wilson@example.com", "5125550187"),
            new("2100 Congress Avenue", "Austin", "TX", "78701", "US"),
            [new(2, "Neon Drift 2088", 49.99m, 1), new(17, "Mechanica Arena", 9.99m, 2)],
            "Standard", 0m, 0m, null, 0.0625m, true, "Amex", "0005",
            ShippedDaysAgo: 6),

        new(5, "D8F3J", 5, "Shipped",
            new("Olivia Brown", "olivia.brown@example.com", "4165550111"),
            new("77 Queen Street West", "Toronto", "ON", "M5H 2N2", "CA"),
            [new(10, "Tidebound", 29.99m, 1), new(15, "Whispering Pines", 22.99m, 1)],
            "Standard", 15.99m, 0m, null, 0m, false, "Visa", "4242",
            ShippedDaysAgo: 4),

        new(6, "M2Q7Z", 3, "Paid",
            new("Liam Davis", "liam.davis@example.com", "6175550134"),
            new("15 Beacon Street", "Boston", "MA", "02108", "US"),
            [new(4, "Kingdoms of Brass", 54.99m, 1), new(8, "The Clockmaker's Riddle", 14.99m, 1)],
            "Standard", 0m, 0m, null, 0.0625m, false, "Discover", "1117"),

        new(7, "T9W4C", 2, "Paid",
            new("Ava Garcia", "ava.garcia@example.com", "3035550176"),
            new("1600 Market Street", "Denver", "CO", "80202", "US"),
            [new(12, "Farmstead Valley", 24.99m, 1), new(18, "Lumen Garden", 12.99m, 1)],
            "Standard", 5.99m, 0m, null, 0.029m, false, "Mastercard", "4444"),

        new(8, "P5L8N", 0.2, "Paid",
            new("Noah Miller", "noah.miller@example.com", "2125550165"),
            new("350 Fifth Avenue", "New York", "NY", "10118", "US"),
            [new(13, "Shadow Protocol", 64.99m, 1), new(9, "Deepwater Station", 34.99m, 1)],
            "Express", 14.99m, 10.00m, "Promo code WELCOME10", 0.04m, true, "Visa", "4242"),

        new(9, "G3H6K", 1.1, "Paid",
            new("Isabella Taylor", "isabella.taylor@example.com", "4045550193"),
            new("900 Peachtree Street NE", "Atlanta", "GA", "30309", "US"),
            [new(11, "Ironclad Legion", 59.99m, 1)],
            "Standard", 0m, 0m, null, 0.04m, true, "Amex", "0005"),

        new(10, "W7X2Y", 15, "Cancelled",
            new("Ethan Anderson", "ethan.anderson@example.com", "3125550148"),
            new("500 North Michigan Avenue", "Chicago", "IL", "60611", "US"),
            [new(6, "Starfall Tactics", 44.99m, 1)],
            "Standard", 5.99m, 0m, null, 0.0625m, false, "Visa", "4242",
            ClosedDaysAgo: 14),

        new(11, "C4D9E", 4, "PaymentFailed",
            new("Mia Thomas", "mia.thomas@example.com", "5035550129"),
            new("123 Main Street", "Portland", "OR", "97204", "US"),
            [new(16, "Rally Kings: Gravel", 39.99m, 1)],
            "Standard", 5.99m, 0m, null, 0m, false, "Visa", "0002",
            DeclineCode: "CARD_DECLINED"),

        new(12, "F6G1H", 1.5, "Paid",
            new("Lucas White", "lucas.white@example.com", "4155550157"),
            new("2400 Fulton Street", "San Francisco", "CA", "94118", "US"),
            [new(20, "Echoes of the Void", 69.99m, 1, IsPreOrder: true), new(5, "Pixel Peaks", 19.99m, 1)],
            "Standard", 0m, 0m, null, 0.0725m, false, "Mastercard", "4444"),

        new(13, "J8K3L", 30, "Delivered",
            new("Charlotte Harris", "charlotte.harris@example.com", "8085550172"),
            new("1 Aloha Tower Drive", "Honolulu", "HI", "96813", "US"),
            [new(14, "Sky Courier", 19.99m, 1)],
            "Standard", 12.99m, 0m, null, 0.04m, true, "Visa", "4242",
            ShippedDaysAgo: 29, DeliveredDaysAgo: 25),

        new(14, "N2P5R", 6, "Delivered",
            new("Benjamin Clark", "benjamin.clark@example.com", "2025550138"),
            new("600 Pennsylvania Avenue SE", "Washington", "DC", "20003", "US"),
            [new(5, "Pixel Peaks", 19.99m, 3)],
            "Standard", 5.99m, 10.00m, "Bundle deal: buy 3, get the cheapest 50% off", 0.06m, false, "Discover", "1117",
            ShippedDaysAgo: 5, DeliveredDaysAgo: 1),

        new(15, "S9T4U", 10, "Shipped",
            new("Amelia Lewis", "amelia.lewis@example.com", "5145550184"),
            new("1000 Sherbrooke Street West", "Montreal", "QC", "H3A 3G4", "CA"),
            [new(18, "Lumen Garden", 12.99m, 2)],
            "Standard", 15.99m, 0m, null, 0m, false, "Amex", "0005",
            ShippedDaysAgo: 9)
    ];
}
