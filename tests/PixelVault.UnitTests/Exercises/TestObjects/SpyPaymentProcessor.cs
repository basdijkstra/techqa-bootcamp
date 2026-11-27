using PixelVault.Payment.Services;

namespace PixelVault.UnitTests.Exercises.TestObjects;

public class SpyPaymentProcessor : IPaymentProcessor
{
    public int NumberOfCharges { get; private set; }

    public Task<ProcessorResult> ChargeAsync(string cardNumber, decimal amount, CancellationToken cancellationToken = default)
    {
        NumberOfCharges++;
        return Task.FromResult(new ProcessorResult(ProcessorOutcome.Approved));
    }
}
