using NUnit.Framework;
using PixelVault.Payment.Domain;
using PixelVault.Payment.Services;

namespace PixelVault.UnitTests.Answers;

[TestFixture]
public class Answers03
{
    [Test]
    public void Assess_AmountOfZero_ThrowsArgumentOutOfRangeException()
    {
        var fraudScorer = new FraudScorer();

        var input = new FraudCheckInput(
            Amount: 0.00m,
            CustomerEmail: "player@example.com",
            PreviousAttemptsWithCard: new List<PaymentTransaction>(),
            Now: new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero));

        /*
         * TODO: add an assertion that verifies that calling Assess with this input
         * throws an ArgumentOutOfRangeException.
         */
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            fraudScorer.Assess(input);
        });
    }

    [Test]
    public void Assess_NegativeAmount_ThrowsArgumentOutOfRangeExceptionForAmount()
    {
        var fraudScorer = new FraudScorer();

        var input = new FraudCheckInput(
            Amount: -10.00m,
            CustomerEmail: "player@example.com",
            PreviousAttemptsWithCard: new List<PaymentTransaction>(),
            Now: new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero));

        /*
         * TODO: add an assertion that verifies that calling Assess with this input
         * throws an ArgumentOutOfRangeException. Store the exception in a variable and also verify that:
         * - its Message contains "must be a non-negative and non-zero value" (how do you verify that a string contains a substring in NUnit?)
         * - its ParamName is "input.Amount"
         * - its ActualValue is the amount you passed in
         */
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            fraudScorer.Assess(input);
        });

        Assert.That(exception!.Message, Does.Contain("must be a non-negative and non-zero value"));
        Assert.That(exception.ParamName, Is.EqualTo("input.Amount"));
        Assert.That(exception.ActualValue, Is.EqualTo(-10.00m));
    }
}
