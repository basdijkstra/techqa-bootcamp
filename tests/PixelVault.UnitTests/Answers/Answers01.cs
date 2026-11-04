using NUnit.Framework;
using PixelVault.Payment.Domain;
using PixelVault.Payment.Services;

namespace PixelVault.UnitTests.Answers;

[TestFixture]
public class Answers01
{
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

        /*
         * TODO: add an assertion that verifies that the resulting assessment score is 45.
         */
        Assert.That(assessment.Score, Is.EqualTo(45));
    }

    [Test]
    public void Assess_LargeAmountWithOnePreviousAttempt_ReturnsScoreOf40()
    {
        var fraudScorer = new FraudScorer();

        var now = new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

        var previousAttempt = new PaymentTransaction
        {
            Status = PaymentStatus.Declined,
            CreatedAt = now.AddMinutes(-5)
        };

        /*
         * TODO: create a FraudCheckInput with the following properties:
         * - Amount: 1250.00m (this should add 40 points to the score)
         * - CustomerEmail: "player@example.com" (this should add 0 points to the score)
         * - PreviousAttemptsWithCard: a list containing the previousAttempt created above
                (this should add 0 points to the score)
         * - Now: the now variable created above
         */
        var input = new FraudCheckInput(
            Amount: 1250.00m,                           // $1,000 or more: +40
            CustomerEmail: "player@example.com",        // regular email domain: +0
            PreviousAttemptsWithCard: new List<PaymentTransaction> { previousAttempt },   // 1 attempt, 1 decline: below both thresholds, +0
            Now: now);

        /*
         * TODO: call the Assess method of the fraudScorer instance,
         * passing in the input created above,
         * and store the result in a variable named assessment.
         */
        var assessment = fraudScorer.Assess(input);

        /*
         * TODO: add an assertion that verifies that the resulting assessment score is 40.
         */
        Assert.That(assessment.Score, Is.EqualTo(40));
    }

    /*
     * TODO: create a new test method named Assess_LargeAmountWithDisposableEmail_ReturnsScoreOf65
     * that tests the following scenario:
     * - Amount: 1500.00m (this should add 40 points to the score)
     * - CustomerEmail: "player@mailinator.com" (this should add 25 points to the score)
     * - PreviousAttemptsWithCard: an empty list (this should add 0 points to the score)
     * - Now: any fixed date and time
     *
     * The expected resulting assessment score should be 65.
     *
     * Use the same structure as the previous test methods, including creating a FraudCheckInput,
     * calling the Assess method, and asserting the resulting score.
     */
    [Test]
    public void Assess_LargeAmountWithDisposableEmail_ReturnsScoreOf65()
    {
        var fraudScorer = new FraudScorer();

        var input = new FraudCheckInput(
            Amount: 1500.00m,                           // $1,000 or more: +40
            CustomerEmail: "player@mailinator.com",     // disposable email domain: +25
            PreviousAttemptsWithCard: new List<PaymentTransaction>(),   // no previous attempts: +0
            Now: new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero));

        var assessment = fraudScorer.Assess(input);

        Assert.That(assessment.Score, Is.EqualTo(65));
    }
}
