using NUnit.Framework;
using PixelVault.UnitTests.Examples.TestObjects;

namespace PixelVault.UnitTests.Examples;

/*
 * WHY
 * The NewsletterSignup class in the TestObjects folder creates its own EmailValidator and SmtpMailer.
 * The tests below work, but:
 * - to get a rejected signup, the test has to know the rules of the EmailValidator (for example, that
 *   mailinator.com addresses are blocked). If those rules change, the NewsletterSignup tests break,
 *   even though nothing changed in NewsletterSignup itself.
 * - every test that signs someone up sends a real email, and we cannot replace the mailer with something else.
 * - we cannot check whether an email was sent, or that no email was sent for a rejected address.
 *
 * WHAT
 * Dependency inversion: a class should not create the objects it depends on, but receive them from outside,
 * preferably as an interface instead of a concrete class. The code that creates the class (the application,
 * or a test) then decides which implementation is used.
 *
 * HOW
 * 1. Extract an interface for each dependency, e.g. IEmailValidator and IMailer, and let EmailValidator and
 *    SmtpMailer implement them.
 * 2. Let NewsletterSignup receive these interfaces through its constructor, instead of creating them itself:
 *
 *        public NewsletterSignup(IEmailValidator emailValidator, IMailer mailer)
 *        {
 *            this.emailValidator = emailValidator;
 *            this.mailer = mailer;
 *        }
 *
 * 3. Create the dependencies in the test and pass them in:
 *
 *        var signup = new NewsletterSignup(new EmailValidator(), new SmtpMailer());
 *
 * The behaviour, and the outcome of the tests, stays exactly the same. But now a test can pass in a different
 * implementation of IEmailValidator or IMailer whenever it needs to. That's what we'll do in the next step.
 */
[TestFixture]
public class Examples04
{
    [Test]
    public void SignUp_ValidEmailAddress_ReturnsTrue()
    {
        var signup = new NewsletterSignup();

        var signedUp = signup.SignUp("reader@example.com");

        Assert.That(signedUp, Is.True);
    }

    [Test]
    public void SignUp_BlockedEmailDomain_ReturnsFalse()
    {
        var signup = new NewsletterSignup();

        var signedUp = signup.SignUp("reader@mailinator.com");

        Assert.That(signedUp, Is.False);
    }
}
