namespace PixelVault.Orders.Services;

public record SalesTaxRule(decimal Rate, bool ShippingIsTaxable);

public interface ISalesTaxCalculator
{
    SalesTaxRule GetRule(string country, string region);

    /// <summary>Tax on the discounted goods, plus shipping in states that tax shipping. Rounded to cents.</summary>
    decimal CalculateTax(string country, string region, decimal taxableGoods, decimal shippingCost);
}

/// <summary>
/// Simplified US sales tax: one statewide base rate per state (no city or county taxes).
/// <list type="bullet">
/// <item>Alaska, Delaware, Montana, New Hampshire and Oregon have no sales tax.</item>
/// <item>Some states also tax shipping; in the others only the goods are taxed.</item>
/// <item>Tax is calculated on the subtotal after discounts.</item>
/// <item>Orders to Canada are exports: no US sales tax. The customer pays import duties on delivery.</item>
/// </list>
/// The rates are realistic, but for training only.
/// </summary>
public class SalesTaxCalculator : ISalesTaxCalculator
{
    private static readonly Dictionary<string, decimal> StateRates = new()
    {
        ["AL"] = 0.04m, ["AK"] = 0m, ["AZ"] = 0.056m, ["AR"] = 0.065m, ["CA"] = 0.0725m, ["CO"] = 0.029m,
        ["CT"] = 0.0635m, ["DE"] = 0m, ["DC"] = 0.06m, ["FL"] = 0.06m, ["GA"] = 0.04m, ["HI"] = 0.04m,
        ["ID"] = 0.06m, ["IL"] = 0.0625m, ["IN"] = 0.07m, ["IA"] = 0.06m, ["KS"] = 0.065m, ["KY"] = 0.06m,
        ["LA"] = 0.05m, ["ME"] = 0.055m, ["MD"] = 0.06m, ["MA"] = 0.0625m, ["MI"] = 0.06m, ["MN"] = 0.06875m,
        ["MS"] = 0.07m, ["MO"] = 0.04225m, ["MT"] = 0m, ["NE"] = 0.055m, ["NV"] = 0.0685m, ["NH"] = 0m,
        ["NJ"] = 0.06625m, ["NM"] = 0.04875m, ["NY"] = 0.04m, ["NC"] = 0.0475m, ["ND"] = 0.05m, ["OH"] = 0.0575m,
        ["OK"] = 0.045m, ["OR"] = 0m, ["PA"] = 0.06m, ["RI"] = 0.07m, ["SC"] = 0.06m, ["SD"] = 0.042m,
        ["TN"] = 0.07m, ["TX"] = 0.0625m, ["UT"] = 0.061m, ["VT"] = 0.06m, ["VA"] = 0.053m, ["WA"] = 0.065m,
        ["WV"] = 0.06m, ["WI"] = 0.05m, ["WY"] = 0.04m
    };

    private static readonly HashSet<string> ShippingTaxableStates =
    [
        "AR", "CT", "GA", "HI", "KS", "KY", "MI", "MN", "MS", "NE", "NJ", "NM", "NY", "NC", "ND", "OH",
        "PA", "RI", "SC", "SD", "TN", "TX", "VT", "WA", "WV", "WI"
    ];

    public SalesTaxRule GetRule(string country, string region)
    {
        if (country == "CA")
        {
            return new SalesTaxRule(0m, false);
        }

        if (country != "US" || !StateRates.TryGetValue(region, out var rate))
        {
            throw new ArgumentException($"No sales tax rule for {region}, {country}.");
        }

        return new SalesTaxRule(rate, ShippingTaxableStates.Contains(region));
    }

    public decimal CalculateTax(string country, string region, decimal taxableGoods, decimal shippingCost)
    {
        var rule = GetRule(country, region);
        var taxable = taxableGoods + (rule.ShippingIsTaxable ? shippingCost : 0m);
        return Math.Round(taxable * rule.Rate, 2, MidpointRounding.AwayFromZero);
    }
}
