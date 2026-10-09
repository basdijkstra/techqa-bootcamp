using System.Text.RegularExpressions;
using PixelVault.Payment.Domain;
using PixelVault.Payment.Dtos;

namespace PixelVault.Payment.Services;

public record CardValidationResult(bool IsValid, CardBrand Brand, string NormalizedNumber, IDictionary<string, string[]> Errors);

public interface ICardValidator
{
    CardValidationResult Validate(CardDetailsDto? card, DateTimeOffset now);

    CardBrand DetectBrand(string digits);

    bool PassesLuhnCheck(string digits);
}

/// <summary>
/// Card rules:
/// <list type="bullet">
/// <item>Spaces and dashes in the number are ignored. The rest must be 12 to 19 digits.</item>
/// <item>Brand by prefix and length: Visa 4 (13, 16 or 19 digits), Mastercard 51-55 or 2221-2720 (16),
/// Amex 34 or 37 (15), Discover 6011, 644-649 or 65 (16 or 19). Other cards are not accepted.</item>
/// <item>The number must pass the Luhn checksum.</item>
/// <item>The CVV is 4 digits for Amex and 3 digits for other brands.</item>
/// <item>The expiry month is 1-12. The year can be 2 digits (26) or 4 digits (2026). A card is valid until the
/// end of its expiry month, and the expiry date cannot be more than 20 years away.</item>
/// <item>The cardholder name is 2 to 100 characters: letters, spaces, hyphens, apostrophes and periods.</item>
/// </list>
/// </summary>
public partial class CardValidator : ICardValidator
{
    public const int MaxYearsAhead = 20;

    [GeneratedRegex(@"^[\p{L} .'-]+$")]
    private static partial Regex NamePattern();

    public CardValidationResult Validate(CardDetailsDto? card, DateTimeOffset now)
    {
        var errors = new Dictionary<string, List<string>>();
        void Add(string field, string message)
        {
            if (!errors.TryGetValue(field, out var list))
            {
                errors[field] = list = [];
            }

            list.Add(message);
        }

        if (card is null)
        {
            Add("card", "Card details are required.");
            return Result(CardBrand.Unknown, "");
        }

        var digits = (card.Number ?? "").Replace(" ", "").Replace("-", "");
        var brand = CardBrand.Unknown;

        if (digits.Length == 0)
        {
            Add("card.number", "Card number is required.");
        }
        else if (!digits.All(char.IsAsciiDigit))
        {
            Add("card.number", "Card number can only contain digits.");
        }
        else if (digits.Length is < 12 or > 19)
        {
            Add("card.number", "Card number must be 12 to 19 digits.");
        }
        else
        {
            brand = DetectBrand(digits);
            if (brand == CardBrand.Unknown)
            {
                Add("card.number", "We accept Visa, Mastercard, American Express and Discover.");
            }
            else if (!PassesLuhnCheck(digits))
            {
                Add("card.number", "Card number is not valid.");
            }
        }

        var cvv = card.Cvv?.Trim() ?? "";
        var expectedCvvLength = brand == CardBrand.Amex ? 4 : 3;
        if (cvv.Length == 0)
        {
            Add("card.cvv", "Security code is required.");
        }
        else if (!cvv.All(char.IsAsciiDigit) || (brand != CardBrand.Unknown && cvv.Length != expectedCvvLength) || cvv.Length is < 3 or > 4)
        {
            Add("card.cvv", brand == CardBrand.Amex
                ? "Security code must be 4 digits for American Express."
                : "Security code must be 3 digits.");
        }

        var year = card.ExpiryYear is >= 0 and < 100 ? 2000 + card.ExpiryYear : card.ExpiryYear;
        if (card.ExpiryMonth is < 1 or > 12)
        {
            Add("card.expiryMonth", "Expiry month must be between 1 and 12.");
        }
        else if (year < 2000 || year > 9998)
        {
            Add("card.expiryYear", "Expiry year is not valid.");
        }
        else
        {
            var firstDayAfterExpiry = new DateTimeOffset(year, card.ExpiryMonth, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(1);
            if (now >= firstDayAfterExpiry)
            {
                Add("card.expiryYear", "This card has expired.");
            }
            else if (firstDayAfterExpiry > now.AddYears(MaxYearsAhead).AddMonths(1))
            {
                Add("card.expiryYear", $"Expiry date cannot be more than {MaxYearsAhead} years from now.");
            }
        }

        var name = card.CardholderName?.Trim() ?? "";
        if (name.Length is < 2 or > 100)
        {
            Add("card.cardholderName", "Cardholder name must be 2 to 100 characters.");
        }
        else if (!NamePattern().IsMatch(name))
        {
            Add("card.cardholderName", "Cardholder name can only contain letters, spaces, hyphens, apostrophes and periods.");
        }

        return Result(brand, digits);

        CardValidationResult Result(CardBrand detectedBrand, string number) =>
            new(errors.Count == 0, detectedBrand, number, errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
    }

    public CardBrand DetectBrand(string digits)
    {
        if (digits.Length < 4 || !digits.All(char.IsAsciiDigit))
        {
            return CardBrand.Unknown;
        }

        var length = digits.Length;
        var two = int.Parse(digits[..2]);
        var three = int.Parse(digits[..3]);
        var four = int.Parse(digits[..4]);

        if (digits[0] == '4' && length is 13 or 16 or 19)
        {
            return CardBrand.Visa;
        }

        if ((two is >= 51 and <= 55 || four is >= 2221 and <= 2720) && length == 16)
        {
            return CardBrand.Mastercard;
        }

        if (two is 34 or 37 && length == 15)
        {
            return CardBrand.Amex;
        }

        if ((four == 6011 || two == 65 || three is >= 644 and <= 649) && length is 16 or 19)
        {
            return CardBrand.Discover;
        }

        return CardBrand.Unknown;
    }

    public bool PassesLuhnCheck(string digits)
    {
        var sum = 0;
        var doubleIt = false;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var digit = digits[i] - '0';
            if (doubleIt)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }

            sum += digit;
            doubleIt = !doubleIt;
        }

        return sum % 10 == 0;
    }
}
