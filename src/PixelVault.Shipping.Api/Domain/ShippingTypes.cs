namespace PixelVault.Shipping.Domain;

public enum ShippingZone
{
    /// <summary>The 48 contiguous states and DC.</summary>
    ContiguousUS,

    /// <summary>Alaska and Hawaii.</summary>
    AlaskaHawaii,

    Canada
}

public enum ShippingMethod
{
    Standard,
    Express
}

public record Region(string Code, string Name);

public record CountryInfo(
    string Code,
    string Name,
    string RegionLabel,
    string PostalCodeLabel,
    string PostalCodeExample,
    IReadOnlyList<Region> Regions);
