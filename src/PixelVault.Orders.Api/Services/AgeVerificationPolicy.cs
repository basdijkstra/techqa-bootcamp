using PixelVault.Shared.Errors;

namespace PixelVault.Orders.Services;

public interface IAgeVerificationPolicy
{
    /// <summary>Throws when the customer is too young for the games in the order.</summary>
    void EnsureOldEnough(int minimumAge, DateOnly? dateOfBirth, DateOnly today);

    int CalculateAge(DateOnly dateOfBirth, DateOnly today);
}

/// <summary>
/// Age check at checkout:
/// <list type="bullet">
/// <item>Only needed when the order contains an M (17+) or AO (18+) game.</item>
/// <item>The customer must then give a date of birth. It cannot be in the future or more than 120 years ago.</item>
/// <item>Age is in whole years. Someone born on February 29 turns a year older on March 1 in non-leap years.</item>
/// </list>
/// </summary>
public class AgeVerificationPolicy : IAgeVerificationPolicy
{
    public const int AgeCheckThreshold = 17;
    public const int MaxAge = 120;

    public void EnsureOldEnough(int minimumAge, DateOnly? dateOfBirth, DateOnly today)
    {
        if (minimumAge < AgeCheckThreshold)
        {
            return;
        }

        if (dateOfBirth is not { } dob)
        {
            throw new ValidationException("ageVerification.dateOfBirth",
                $"Your order contains games rated for ages {minimumAge}+. Please enter your date of birth.");
        }

        if (dob > today)
        {
            throw new ValidationException("ageVerification.dateOfBirth", "Date of birth cannot be in the future.");
        }

        var age = CalculateAge(dob, today);
        if (age > MaxAge)
        {
            throw new ValidationException("ageVerification.dateOfBirth", "Please enter a valid date of birth.");
        }

        if (age < minimumAge)
        {
            var exception = new BusinessRuleException("AGE_REQUIREMENT_NOT_MET",
                $"You must be {minimumAge} or older to buy the games in your basket.");
            exception.Details["minimumAge"] = minimumAge;
            throw exception;
        }
    }

    public int CalculateAge(DateOnly dateOfBirth, DateOnly today)
    {
        var age = today.Year - dateOfBirth.Year;
        var birthdayThisYear = dateOfBirth.Month == 2 && dateOfBirth.Day == 29 && !DateTime.IsLeapYear(today.Year)
            ? new DateOnly(today.Year, 3, 1)
            : new DateOnly(today.Year, dateOfBirth.Month, dateOfBirth.Day);

        return today < birthdayThisYear ? age - 1 : age;
    }
}
