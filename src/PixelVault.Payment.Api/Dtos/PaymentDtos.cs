using PixelVault.Payment.Domain;

namespace PixelVault.Payment.Dtos;

public record CardDetailsDto(string? Number, string? CardholderName, int ExpiryMonth, int ExpiryYear, string? Cvv);

public record CreatePaymentRequest(string? OrderReference, decimal Amount, string? Currency, string? CustomerEmail, CardDetailsDto? Card);

public record RefundDto(Guid Id, decimal Amount, string? Reason, DateTimeOffset CreatedAt);

public record PaymentDto(
    Guid Id,
    string OrderReference,
    decimal Amount,
    string Currency,
    PaymentStatus Status,
    CardBrand CardBrand,
    string CardLast4,
    string? DeclineCode,
    int FraudScore,
    decimal RefundedAmount,
    DateTimeOffset CreatedAt,
    IReadOnlyList<RefundDto> Refunds);

public record CreateRefundRequest(decimal Amount, string? Reason);

public record FaultInjectionSettingsDto(double FailureRate, int ExtraLatencyMilliseconds);
