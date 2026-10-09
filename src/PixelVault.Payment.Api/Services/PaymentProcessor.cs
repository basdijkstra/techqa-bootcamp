namespace PixelVault.Payment.Services;

public enum ProcessorOutcome
{
    Approved,
    Declined,
    Error
}

public record ProcessorResult(ProcessorOutcome Outcome, string? DeclineCode = null, string? Message = null);

public interface IPaymentProcessor
{
    /// <summary>Sends the charge to the (simulated) card network.</summary>
    Task<ProcessorResult> ChargeAsync(string cardNumber, decimal amount, CancellationToken cancellationToken = default);
}

/// <summary>
/// A fake card network. Every valid card is approved, except these test cards:
/// <code>
/// 4000 0000 0000 0002   declined: CARD_DECLINED
/// 4000 0000 0000 9995   declined: INSUFFICIENT_FUNDS
/// 4000 0000 0000 0069   declined: EXPIRED_CARD
/// 4000 0000 0000 0127   declined: INCORRECT_CVC
/// 4100 0000 0000 0019   declined: FRAUD_SUSPECTED (always blocked)
/// 4000 0000 0000 0119   processor error (no money taken)
/// 4000 0000 0000 0259   approved after a 5 second delay
/// </code>
/// </summary>
public class SimulatedPaymentProcessor(TimeProvider timeProvider) : IPaymentProcessor
{
    public static readonly TimeSpan SlowCardDelay = TimeSpan.FromSeconds(5);

    public static readonly IReadOnlyDictionary<string, string> DeclinedCards = new Dictionary<string, string>
    {
        ["4000000000000002"] = "CARD_DECLINED",
        ["4000000000009995"] = "INSUFFICIENT_FUNDS",
        ["4000000000000069"] = "EXPIRED_CARD",
        ["4000000000000127"] = "INCORRECT_CVC",
        ["4100000000000019"] = "FRAUD_SUSPECTED"
    };

    public const string ProcessorErrorCard = "4000000000000119";
    public const string SlowCard = "4000000000000259";

    public async Task<ProcessorResult> ChargeAsync(string cardNumber, decimal amount, CancellationToken cancellationToken = default)
    {
        if (cardNumber == SlowCard)
        {
            await Task.Delay(SlowCardDelay, timeProvider, cancellationToken);
            return new ProcessorResult(ProcessorOutcome.Approved);
        }

        if (cardNumber == ProcessorErrorCard)
        {
            return new ProcessorResult(ProcessorOutcome.Error, Message: "The card network did not respond.");
        }

        return DeclinedCards.TryGetValue(cardNumber, out var declineCode)
            ? new ProcessorResult(ProcessorOutcome.Declined, declineCode)
            : new ProcessorResult(ProcessorOutcome.Approved);
    }
}
