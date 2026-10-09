using System.Text.RegularExpressions;
using PixelVault.Orders.Dtos;
using PixelVault.Shared.Errors;

namespace PixelVault.Orders.Services;

public record ValidCustomerDetails(string FullName, string Email, string Phone);

public interface ICustomerDetailsValidator
{
    /// <summary>Validates and normalizes the customer details. Throws a 400 with every problem found.</summary>
    ValidCustomerDetails Validate(CustomerDetailsDto? customer);
}

/// <summary>
/// Customer rules:
/// <list type="bullet">
/// <item>Full name: 2 to 100 characters, with at least a first and a last name.</item>
/// <item>Email: one @, something before it, a domain with a dot after it, at most 254 characters. Stored in lower case.</item>
/// <item>Phone: a North American number. Spaces, dashes, dots, brackets and a leading +1 or 1 are ignored.
/// What's left must be 10 digits, and the area code and exchange (digits 1 and 4) cannot start with 0 or 1.</item>
/// </list>
/// </summary>
public partial class CustomerDetailsValidator : ICustomerDetailsValidator
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    public ValidCustomerDetails Validate(CustomerDetailsDto? customer)
    {
        var errors = new ValidationErrors();
        if (customer is null)
        {
            errors.Add("customer", "Customer details are required.");
            errors.ThrowIfAny();
        }

        var name = string.Join(' ', (customer!.FullName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (name.Length is < 2 or > 100)
        {
            errors.Add("customer.fullName", "Full name must be 2 to 100 characters.");
        }
        else if (!name.Contains(' '))
        {
            errors.Add("customer.fullName", "Please enter your first and last name.");
        }

        var email = customer.Email?.Trim().ToLowerInvariant() ?? "";
        if (email.Length == 0)
        {
            errors.Add("customer.email", "Email address is required.");
        }
        else if (email.Length > 254 || !EmailPattern().IsMatch(email))
        {
            errors.Add("customer.email", "Email address is not valid.");
        }

        var phone = NormalizePhone(customer.Phone);
        if (string.IsNullOrWhiteSpace(customer.Phone))
        {
            errors.Add("customer.phone", "Phone number is required.");
        }
        else if (phone is null)
        {
            errors.Add("customer.phone", "Phone number must be a 10 digit US or Canadian number, e.g. (212) 555-0123.");
        }

        errors.ThrowIfAny();
        return new ValidCustomerDetails(name, email, phone!);
    }

    /// <summary>Returns the 10 digit phone number, or null when it is not a valid North American number.</summary>
    public static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var allowedSeparators = " -.()+";
        if (phone.Any(c => !char.IsAsciiDigit(c) && !allowedSeparators.Contains(c)))
        {
            return null;
        }

        var digits = new string(phone.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length == 11 && digits[0] == '1')
        {
            digits = digits[1..];
        }

        if (digits.Length != 10 || digits[0] is '0' or '1' || digits[3] is '0' or '1')
        {
            return null;
        }

        return digits;
    }
}
