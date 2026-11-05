using NUnit.Framework;
using PixelVault.Payment.Domain;
using PixelVault.Payment.Services;

namespace PixelVault.UnitTests.Exercises;

[TestFixture]
public class Exercises02
{
    /*
     * The three tests below verify the same logic (the fraud score calculation) with different data.
     * They only differ in the amount, the customer email address and the expected score.
     */
    [Test]
    public void Assess_MidSizedAmountWithDisposableEmail_ReturnsScoreOf45()
    {
        var fraudScorer = new FraudScorer();

        var input = new FraudCheckInput(
            Amount: 750.00m,                            // $500 up to $999.99: +20
            CustomerEmail: "player@mailinator.com",     // disposable email domain: +25
            PreviousAttemptsWithCard: new List<PaymentTransaction>(),
            Now: new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero));

        var assessment = fraudScorer.Assess(input);

        Assert.That(assessment.Score, Is.EqualTo(45));
    }

    [Test]
    public void Assess_LargeAmountWithRegularEmail_ReturnsScoreOf40()
    {
        var fraudScorer = new FraudScorer();

        var input = new FraudCheckInput(
            Amount: 1250.00m,                           // $1,000 or more: +40
            CustomerEmail: "player@example.com",        // regular email domain: +0
            PreviousAttemptsWithCard: new List<PaymentTransaction>(),
            Now: new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero));

        var assessment = fraudScorer.Assess(input);

        Assert.That(assessment.Score, Is.EqualTo(40));
    }

    [Test]
    public void Assess_LargeAmountWithDisposableEmail_ReturnsScoreOf65()
    {
        var fraudScorer = new FraudScorer();

        var input = new FraudCheckInput(
            Amount: 1500.00m,                           // $1,000 or more: +40
            CustomerEmail: "player@mailinator.com",     // disposable email domain: +25
            PreviousAttemptsWithCard: new List<PaymentTransaction>(),
            Now: new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero));

        var assessment = fraudScorer.Assess(input);

        Assert.That(assessment.Score, Is.EqualTo(65));
    }

    /*
     * TODO: refactor the three tests above into a single parameterized test using [TestCase].
     * The test method should take the amount, the customer email address and the expected score as parameters.
     * Give every test case a readable name using TestName.
     *
     * Note: C# does not allow decimal values in attributes. Pass the amount as a double (e.g. 750.00)
     * and declare the parameter as decimal: NUnit converts the value for you.
     */


    /*
     * TODO: refactor the three tests above into a single parameterized test using [TestCaseSource].
     * Create a private static method named FraudScoreTestData that returns an IEnumerable<TestCaseData>,
     * with one TestCaseData object per test case. Give every test case a readable name using SetName().
     *
     * Note: unlike attributes, TestCaseData accepts decimal values, so you can use 750.00m here.
     */

}