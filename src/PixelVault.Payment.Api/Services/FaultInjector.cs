using PixelVault.Payment.Dtos;
using PixelVault.Shared.Errors;

namespace PixelVault.Payment.Services;

public interface IFaultInjector
{
    FaultInjectionSettingsDto Settings { get; }

    void Configure(FaultInjectionSettingsDto settings);

    /// <summary>Adds the configured delay, then fails randomly at the configured rate.</summary>
    Task ApplyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Makes the Payment service slow or unreliable on purpose, to test how the rest of the system copes.
/// Configured in appsettings (<c>FaultInjection</c>) or at runtime with <c>PUT /test-support/faults</c>.
/// </summary>
public class FaultInjector : IFaultInjector
{
    private readonly Lock _lock = new();
    private FaultInjectionSettingsDto _settings;

    public FaultInjector(IConfiguration configuration)
    {
        _settings = new FaultInjectionSettingsDto(
            configuration.GetValue("FaultInjection:FailureRate", 0.0),
            configuration.GetValue("FaultInjection:ExtraLatencyMilliseconds", 0));
    }

    public FaultInjectionSettingsDto Settings
    {
        get
        {
            lock (_lock)
            {
                return _settings;
            }
        }
    }

    public void Configure(FaultInjectionSettingsDto settings)
    {
        var errors = new ValidationErrors();
        errors.AddIf(settings.FailureRate is < 0 or > 1, "failureRate", "Failure rate must be between 0 and 1.");
        errors.AddIf(settings.ExtraLatencyMilliseconds is < 0 or > 30000, "extraLatencyMilliseconds", "Extra latency must be between 0 and 30000 ms.");
        errors.ThrowIfAny();

        lock (_lock)
        {
            _settings = settings;
        }
    }

    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        var settings = Settings;
        if (settings.ExtraLatencyMilliseconds > 0)
        {
            await Task.Delay(settings.ExtraLatencyMilliseconds, cancellationToken);
        }

        if (settings.FailureRate > 0 && Random.Shared.NextDouble() < settings.FailureRate)
        {
            throw new DomainStatusException(503, "PAYMENT_SERVICE_UNAVAILABLE", "The payment service is temporarily unavailable (fault injection).");
        }
    }
}
