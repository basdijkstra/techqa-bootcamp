namespace PixelVault.UnitTests.Examples.TestObjects;

/// <summary>Checks whether an email address can be used to sign up for the newsletter.</summary>
public class EmailValidator
{
    private static readonly string[] BlockedDomains = ["mailinator.com", "trashmail.com"];

    public bool IsValid(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2 || parts[0].Length == 0 || !parts[1].Contains('.'))
        {
            return false;
        }

        return !BlockedDomains.Contains(parts[1].ToLowerInvariant());
    }
}

/// <summary>Sends emails. Imagine this talks to a real mail server.</summary>
public class SmtpMailer
{
    public bool Send(string to, string subject)
    {
        Console.WriteLine($"Sending '{subject}' to {to}");
        return true;
    }
}

/// <summary>
/// Signs up a reader for the newsletter: checks the email address, then sends a welcome email.
/// Notice how this class creates its own EmailValidator and SmtpMailer.
/// </summary>
public class NewsletterSignup
{
    private readonly EmailValidator emailValidator;
    private readonly SmtpMailer mailer;

    public NewsletterSignup()
    {
        emailValidator = new EmailValidator();
        mailer = new SmtpMailer();
    }

    /// <summary>Returns true when the reader was signed up and the welcome email was sent.</summary>
    public bool SignUp(string email)
    {
        if (!emailValidator.IsValid(email))
        {
            return false;
        }

        return mailer.Send(email, "Welcome to our newsletter!");
    }
}
