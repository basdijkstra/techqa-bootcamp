using System.Security.Cryptography;
using System.Text;
using PixelVault.Payment.Domain;
using PixelVault.Payment.Dtos;
using PixelVault.Payment.Repositories;
using PixelVault.Shared.Errors;

namespace PixelVault.Payment.Services;

public interface IPaymentService
{
    /// <summary>Charges a card. Returns the payment and whether it was created now (false for an idempotent replay).</summary>
    Task<(PaymentDto Payment, bool Created)> CreatePaymentAsync(string? idempotencyKey, CreatePaymentRequest request, CancellationToken cancellationToken = default);

    Task<PaymentDto> GetAsync(Guid paymentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentDto>> GetByOrderReferenceAsync(string orderReference, CancellationToken cancellationToken = default);

    Task<RefundDto> RefundAsync(Guid paymentId, CreateRefundRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Payment rules:
/// <list type="bullet">
/// <item>Every request needs an <c>Idempotency-Key</c> header. Repeating a request with the same key returns the
/// first result and never charges twice. Reusing a key for a different order or amount is a 409.</item>
/// <item>Amounts are in USD, between $0.01 and $2,500.00, with at most 2 decimals.</item>
/// <item>Card details are validated first (400), then the fraud check runs (declined at 70 points or more),
/// then the card network is called.</item>
/// <item>Declined payments return 402 with a decline code. Processor errors return 502. Both are stored.</item>
/// <item>Refunds are only possible for captured payments, must be more than $0, and the total refunded can never be
/// more than the amount paid.</item>
/// </list>
/// </summary>
public class PaymentService(
    IPaymentRepository payments,
    ICardValidator cardValidator,
    IFraudScorer fraudScorer,
    IPaymentProcessor processor,
    IFaultInjector faultInjector,
    TimeProvider timeProvider) : IPaymentService
{
    public const decimal MinAmount = 0.01m;
    public const decimal MaxAmount = 2500.00m;
    public const int MaxIdempotencyKeyLength = 100;

    public async Task<(PaymentDto Payment, bool Created)> CreatePaymentAsync(string? idempotencyKey, CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var key = idempotencyKey?.Trim();
        if (string.IsNullOrEmpty(key) || key.Length > MaxIdempotencyKeyLength)
        {
            throw new ValidationException("Idempotency-Key", $"The Idempotency-Key header is required (at most {MaxIdempotencyKeyLength} characters).");
        }

        var existing = await payments.GetByIdempotencyKeyAsync(key, cancellationToken);
        if (existing is not null)
        {
            if (existing.OrderReference != request.OrderReference?.Trim() || existing.Amount != request.Amount)
            {
                throw new ConflictException("IDEMPOTENCY_KEY_REUSED", "This Idempotency-Key was already used for a different payment.");
            }

            return (Replay(existing), false);
        }

        var now = timeProvider.GetUtcNow();
        var card = ValidateRequest(request, now);

        await faultInjector.ApplyAsync(cancellationToken);

        var fingerprint = Fingerprint(card.NormalizedNumber);
        var previousAttempts = await payments.GetByCardFingerprintSinceAsync(fingerprint, now.AddHours(-24), cancellationToken);
        var fraud = fraudScorer.Assess(new FraudCheckInput(request.Amount, request.CustomerEmail, previousAttempts, now));

        var payment = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = key,
            OrderReference = request.OrderReference!.Trim(),
            Amount = request.Amount,
            Currency = "USD",
            CardBrand = card.Brand,
            CardLast4 = card.NormalizedNumber[^4..],
            CardFingerprint = fingerprint,
            CardholderName = request.Card!.CardholderName!.Trim(),
            CustomerEmail = request.CustomerEmail?.Trim(),
            FraudScore = fraud.Score,
            CreatedAt = now
        };

        if (fraud.IsBlocked)
        {
            payment.Status = PaymentStatus.Declined;
            payment.DeclineCode = "FRAUD_SUSPECTED";
        }
        else
        {
            var result = await processor.ChargeAsync(card.NormalizedNumber, request.Amount, cancellationToken);
            (payment.Status, payment.DeclineCode) = result.Outcome switch
            {
                ProcessorOutcome.Approved => (PaymentStatus.Captured, (string?)null),
                ProcessorOutcome.Declined => (PaymentStatus.Declined, result.DeclineCode),
                _ => (PaymentStatus.Failed, "PROCESSOR_ERROR")
            };
        }

        payments.Add(payment);
        await payments.SaveChangesAsync(cancellationToken);

        return (Replay(payment), true);
    }

    public async Task<PaymentDto> GetAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await payments.GetAsync(paymentId, cancellationToken) ?? throw new NotFoundException("Payment", paymentId);
        return ToDto(payment);
    }

    public async Task<IReadOnlyList<PaymentDto>> GetByOrderReferenceAsync(string orderReference, CancellationToken cancellationToken = default)
    {
        var found = await payments.GetByOrderReferenceAsync(orderReference.Trim(), cancellationToken);
        return found.Select(ToDto).ToList();
    }

    public async Task<RefundDto> RefundAsync(Guid paymentId, CreateRefundRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new ValidationErrors();
        errors.AddIf(request.Amount <= 0, "amount", "Refund amount must be more than $0.00.");
        errors.AddIf(decimal.Round(request.Amount, 2) != request.Amount, "amount", "Refund amount can have at most 2 decimals.");
        errors.AddIf(request.Reason?.Length > 200, "reason", "Reason cannot be longer than 200 characters.");
        errors.ThrowIfAny();

        var payment = await payments.GetAsync(paymentId, cancellationToken) ?? throw new NotFoundException("Payment", paymentId);

        if (payment.Status is not (PaymentStatus.Captured or PaymentStatus.PartiallyRefunded))
        {
            throw new BusinessRuleException("PAYMENT_NOT_REFUNDABLE", $"A payment with status {payment.Status} cannot be refunded.");
        }

        var refundable = payment.Amount - payment.RefundedAmount;
        if (request.Amount > refundable)
        {
            var exception = new BusinessRuleException("REFUND_EXCEEDS_PAYMENT", $"At most {refundable:C} can still be refunded.");
            exception.Details["refundableAmount"] = refundable;
            throw exception;
        }

        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            PaymentId = payment.Id,
            Amount = request.Amount,
            Reason = request.Reason?.Trim(),
            CreatedAt = timeProvider.GetUtcNow()
        };

        payment.Refunds.Add(refund);
        payments.AddRefund(refund);
        payment.RefundedAmount += request.Amount;
        payment.Status = payment.RefundedAmount == payment.Amount ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
        await payments.SaveChangesAsync(cancellationToken);

        return new RefundDto(refund.Id, refund.Amount, refund.Reason, refund.CreatedAt);
    }

    private CardValidationResult ValidateRequest(CreatePaymentRequest request, DateTimeOffset now)
    {
        var errors = new ValidationErrors();
        errors.AddIf(string.IsNullOrWhiteSpace(request.OrderReference), "orderReference", "Order reference is required.");
        errors.AddIf(request.Amount is < MinAmount or > MaxAmount, "amount", $"Amount must be between {MinAmount:C} and {MaxAmount:C}.");
        errors.AddIf(decimal.Round(request.Amount, 2) != request.Amount, "amount", "Amount can have at most 2 decimals.");
        errors.AddIf(!string.Equals(request.Currency ?? "USD", "USD", StringComparison.OrdinalIgnoreCase), "currency", "Only USD is supported.");

        var card = cardValidator.Validate(request.Card, now);
        foreach (var (field, messages) in card.Errors)
        {
            foreach (var message in messages)
            {
                errors.Add(field, message);
            }
        }

        errors.ThrowIfAny();
        return card;
    }

    /// <summary>Turns a stored payment into the response for the request: 2xx, 402 or 502.</summary>
    private static PaymentDto Replay(PaymentTransaction payment)
    {
        switch (payment.Status)
        {
            case PaymentStatus.Declined:
            {
                var exception = new DomainStatusException(402, "PAYMENT_DECLINED", DeclineMessage(payment.DeclineCode));
                exception.Details["paymentId"] = payment.Id;
                exception.Details["declineCode"] = payment.DeclineCode;
                throw exception;
            }
            case PaymentStatus.Failed:
            {
                var exception = new DomainStatusException(502, "PROCESSOR_ERROR", "The payment could not be processed. No money was taken. Please try again.");
                exception.Details["paymentId"] = payment.Id;
                throw exception;
            }
            default:
                return ToDto(payment);
        }
    }

    private static string DeclineMessage(string? declineCode) => declineCode switch
    {
        "INSUFFICIENT_FUNDS" => "Your card has insufficient funds.",
        "EXPIRED_CARD" => "Your card has expired.",
        "INCORRECT_CVC" => "Your card's security code is incorrect.",
        "FRAUD_SUSPECTED" => "This payment was declined by our fraud check.",
        _ => "Your card was declined."
    };

    private static string Fingerprint(string cardNumber) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cardNumber)));

    private static PaymentDto ToDto(PaymentTransaction p) => new(
        p.Id,
        p.OrderReference,
        p.Amount,
        p.Currency,
        p.Status,
        p.CardBrand,
        p.CardLast4,
        p.DeclineCode,
        p.FraudScore,
        p.RefundedAmount,
        p.CreatedAt,
        p.Refunds.OrderBy(r => r.CreatedAt).Select(r => new RefundDto(r.Id, r.Amount, r.Reason, r.CreatedAt)).ToList());
}
