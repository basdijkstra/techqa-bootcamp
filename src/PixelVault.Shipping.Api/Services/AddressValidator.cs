using System.Text.RegularExpressions;
using PixelVault.Shipping.Domain;
using PixelVault.Shipping.Dtos;
using PixelVault.Shared.Errors;

namespace PixelVault.Shipping.Services;

public interface IAddressValidator
{
    AddressValidationResultDto Validate(AddressDto? address);
}

/// <summary>
/// Address rules:
/// <list type="bullet">
/// <item>Line 1 and city are required. Line 1 and line 2 are at most 100 characters, city at most 60.</item>
/// <item>Country must be US or CA. Region must be a state/DC (US) or a province/territory (CA).</item>
/// <item>US ZIP codes are 5 digits, optionally followed by a dash and 4 digits (12345 or 12345-6789).</item>
/// <item>Canadian postal codes look like A1A 1A1. The space is optional. D, F, I, O, Q and U are never used,
/// and W and Z are never the first letter.</item>
/// <item>The ZIP code must belong to the state (by its first three digits) and the postal code to the province
/// (by its first letter).</item>
/// </list>
/// The normalized address is trimmed, with region and postal code in upper case and a space in Canadian postal codes.
/// </summary>
public partial class AddressValidator : IAddressValidator
{
    public const int MaxLineLength = 100;
    public const int MaxCityLength = 60;

    [GeneratedRegex(@"^\d{5}(-\d{4})?$")]
    private static partial Regex UsZipCode();

    [GeneratedRegex(@"^[ABCEGHJ-NPRSTVXY]\d[ABCEGHJ-NPRSTV-Z] ?\d[ABCEGHJ-NPRSTV-Z]\d$")]
    private static partial Regex CanadianPostalCode();

    public AddressValidationResultDto Validate(AddressDto? address)
    {
        var errors = new ValidationErrors();
        if (address is null)
        {
            errors.Add("address", "Address is required.");
            return new AddressValidationResultDto(false, errors.ToDictionary(), null);
        }

        var line1 = address.Line1?.Trim() ?? "";
        var line2 = string.IsNullOrWhiteSpace(address.Line2) ? null : address.Line2.Trim();
        var city = address.City?.Trim() ?? "";
        var country = address.Country?.Trim().ToUpperInvariant() ?? "";
        var region = address.Region?.Trim().ToUpperInvariant() ?? "";
        var postalCode = address.PostalCode?.Trim().ToUpperInvariant() ?? "";

        errors.AddIf(line1.Length == 0, "line1", "Address line 1 is required.");
        errors.AddIf(line1.Length > MaxLineLength, "line1", $"Address line 1 cannot be longer than {MaxLineLength} characters.");
        errors.AddIf(line2?.Length > MaxLineLength, "line2", $"Address line 2 cannot be longer than {MaxLineLength} characters.");
        errors.AddIf(city.Length == 0, "city", "City is required.");
        errors.AddIf(city.Length > MaxCityLength, "city", $"City cannot be longer than {MaxCityLength} characters.");

        switch (country)
        {
            case "":
                errors.Add("country", "Country is required.");
                break;
            case Countries.UnitedStates:
                ValidateUs(region, postalCode, errors);
                break;
            case Countries.Canada:
                postalCode = ValidateCanada(region, postalCode, errors);
                break;
            default:
                errors.Add("country", "We only ship to the United States (US) and Canada (CA).");
                break;
        }

        if (errors.HasErrors)
        {
            return new AddressValidationResultDto(false, errors.ToDictionary(), null);
        }

        return new AddressValidationResultDto(
            true,
            new Dictionary<string, string[]>(),
            new AddressDto(line1, line2, city, region, postalCode, country));
    }

    private static void ValidateUs(string state, string zipCode, ValidationErrors errors)
    {
        var stateKnown = Countries.UsStates.Any(s => s.Code == state);
        if (state.Length == 0)
        {
            errors.Add("region", "State is required.");
        }
        else if (!stateKnown)
        {
            errors.Add("region", $"'{state}' is not a US state.");
        }

        if (zipCode.Length == 0)
        {
            errors.Add("postalCode", "ZIP code is required.");
            return;
        }

        if (!UsZipCode().IsMatch(zipCode))
        {
            errors.Add("postalCode", "ZIP code must be 5 digits, or 5 digits, a dash and 4 digits (12345-6789).");
            return;
        }

        if (stateKnown)
        {
            var prefix = int.Parse(zipCode[..3]);
            var matches = Countries.UsZipPrefixes[state].Any(range => prefix >= range.From && prefix <= range.To);
            errors.AddIf(!matches, "postalCode", $"ZIP code {zipCode} is not in {state}.");
        }
    }

    private static string ValidateCanada(string province, string postalCode, ValidationErrors errors)
    {
        var provinceKnown = Countries.CanadianProvinces.Any(p => p.Code == province);
        if (province.Length == 0)
        {
            errors.Add("region", "Province is required.");
        }
        else if (!provinceKnown)
        {
            errors.Add("region", $"'{province}' is not a Canadian province or territory.");
        }

        if (postalCode.Length == 0)
        {
            errors.Add("postalCode", "Postal code is required.");
            return postalCode;
        }

        if (!CanadianPostalCode().IsMatch(postalCode))
        {
            errors.Add("postalCode", "Postal code must look like A1A 1A1.");
            return postalCode;
        }

        var normalized = postalCode.Length == 6 ? $"{postalCode[..3]} {postalCode[3..]}" : postalCode;
        if (provinceKnown)
        {
            var matches = Countries.CanadianPostalPrefixes[province].Contains(normalized[0]);
            errors.AddIf(!matches, "postalCode", $"Postal code {normalized} is not in {province}.");
        }

        return normalized;
    }
}
