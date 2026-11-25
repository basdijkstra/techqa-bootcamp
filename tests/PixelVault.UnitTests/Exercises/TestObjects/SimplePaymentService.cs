using PixelVault.Payment.Domain;
using PixelVault.Payment.Services;

namespace PixelVault.UnitTests.Exercises.TestObjects;

/// <summary>The outcome of a payment attempt.</summary>
public record PaymentResult(PaymentStatus Status, string? DeclineCode, int FraudScore);

/// <summary>
/// A trimmed version of <see cref="PaymentService"/> that only covers the fraud check and the call to the
/// payment processor. Validation, idempotency, time and storage are left out: the caller passes in the previous
/// attempts with the same card, which the real PaymentService gets from its repository.
/// <list type="bullet">
/// <item>The fraud check runs first. A payment with a score of 70 or more is declined with FRAUD_SUSPECTED,
/// and the payment processor is not called.</item>
/// <item>Otherwise the card is charged: approved becomes Captured, declined keeps the processor's decline code,
/// and a processor error becomes Failed with PROCESSOR_ERROR.</item>
/// </list>
/// </summary>
/*
 * TODO: this class creates its own dependencies, which makes it hard to test.
 * Refactor it so that the fraud scorer and the payment processor are passed in through the constructor
 * as an IFraudScorer and an IPaymentProcessor, instead of being created inside the class.
 * The behaviour of the ChargeAsync method should not change.
 * After that, update the existing tests in Exercises04 so they create the real dependencies and pass them in.
 */
public class SimplePaymentService
{
    private readonly FraudScorer fraudScorer;
    private readonly SimulatedPaymentProcessor processor;

    public SimplePaymentService()
    {
        fraudScorer = new FraudScorer();
        processor = new SimulatedPaymentProcessor(TimeProvider.System);
    }

    public async Task<PaymentResult> ChargeAsync(decimal amount, string? customerEmail, string cardNumber,
        IReadOnlyCollection<PaymentTransaction> previousAttemptsWithCard, CancellationToken cancellationToken = default)
    {
        var fraud = fraudScorer.Assess(new FraudCheckInput(amount, customerEmail, previousAttemptsWithCard, DateTimeOffset.UtcNow));

        if (fraud.IsBlocked)
        {
            return new PaymentResult(PaymentStatus.Declined, "FRAUD_SUSPECTED", fraud.Score);
        }

        var result = await processor.ChargeAsync(cardNumber, amount, cancellationToken);

        return result.Outcome switch
        {
            ProcessorOutcome.Approved => new PaymentResult(PaymentStatus.Captured, null, fraud.Score),
            ProcessorOutcome.Declined => new PaymentResult(PaymentStatus.Declined, result.DeclineCode, fraud.Score),
            _ => new PaymentResult(PaymentStatus.Failed, "PROCESSOR_ERROR", fraud.Score)
        };
    }
}
