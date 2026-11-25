using NUnit.Framework;
using PixelVault.Payment.Domain;
using PixelVault.Payment.Services;
using PixelVault.UnitTests.Answers.TestObjects;

namespace PixelVault.UnitTests.Answers;

/*
 * The SimplePaymentService class in the TestObjects folder creates its own FraudScorer and SimulatedPaymentProcessor.
 * The tests below work, but look at what they need to know to control the outcome:
 * - the declined and processor error tests depend on 'magic' test card numbers that the SimulatedPaymentProcessor recognizes
 * - the fraud check test depends on the scoring rules of the FraudScorer: an amount of $1,000 or more (+40)
 *   and 2 declined attempts with the same card in the last 24 hours (+30) add up to exactly the threshold of 70
 *
 * TODO: refactor the SimplePaymentService class so that it receives an IFraudScorer and an IPaymentProcessor
 * through its constructor (see the TODO in that class), then update the tests below:
 * create a real FraudScorer and a real SimulatedPaymentProcessor in each test, and pass them in.
 * Use TimeProvider.System as the argument for the SimulatedPaymentProcessor constructor.
 * The tests should still pass after the refactoring.
 */
[TestFixture]
public class Answers04
{
    [Test]
    public async Task ChargeAsync_RegularCard_IsCaptured()
    {
        var paymentService = new SimplePaymentService(new FraudScorer(), new SimulatedPaymentProcessor(TimeProvider.System));

        var result = await paymentService.ChargeAsync(100.00m, "player@example.com", "4242424242424242", []);

        Assert.That(result.Status, Is.EqualTo(PaymentStatus.Captured));
        Assert.That(result.DeclineCode, Is.Null);
    }

    [Test]
    public async Task ChargeAsync_DeclinedTestCard_IsDeclinedWithCardDeclined()
    {
        var paymentService = new SimplePaymentService(new FraudScorer(), new SimulatedPaymentProcessor(TimeProvider.System));

        var result = await paymentService.ChargeAsync(100.00m, "player@example.com", "4000000000000002", []);

        Assert.That(result.Status, Is.EqualTo(PaymentStatus.Declined));
        Assert.That(result.DeclineCode, Is.EqualTo("CARD_DECLINED"));
    }

    [Test]
    public async Task ChargeAsync_ProcessorErrorTestCard_FailsWithProcessorError()
    {
        var paymentService = new SimplePaymentService(new FraudScorer(), new SimulatedPaymentProcessor(TimeProvider.System));

        var result = await paymentService.ChargeAsync(100.00m, "player@example.com", "4000000000000119", []);

        Assert.That(result.Status, Is.EqualTo(PaymentStatus.Failed));
        Assert.That(result.DeclineCode, Is.EqualTo("PROCESSOR_ERROR"));
    }

    [Test]
    public async Task ChargeAsync_LargeAmountAfterTwoRecentDeclines_IsBlockedByFraudCheck()
    {
        var paymentService = new SimplePaymentService(new FraudScorer(), new SimulatedPaymentProcessor(TimeProvider.System));

        var previousAttempts = new List<PaymentTransaction>
        {
            new() { Status = PaymentStatus.Declined, CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10) },
            new() { Status = PaymentStatus.Declined, CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5) }
        };

        var result = await paymentService.ChargeAsync(1250.00m, "player@example.com", "4242424242424242", previousAttempts);

        Assert.That(result.Status, Is.EqualTo(PaymentStatus.Declined));
        Assert.That(result.DeclineCode, Is.EqualTo("FRAUD_SUSPECTED"));
        Assert.That(result.FraudScore, Is.EqualTo(70));
    }
}
