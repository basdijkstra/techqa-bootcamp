using System.Net;
using System.Text.Json;
using PixelVault.Shared.Errors;

namespace PixelVault.Orders.Clients;

public record CardDetails(string? Number, string? CardholderName, int ExpiryMonth, int ExpiryYear, string? Cvv);

public record ChargeRequest(string OrderReference, decimal Amount, string CustomerEmail, CardDetails Card);

public enum ChargeOutcome
{
    Approved,
    Declined
}

public record ChargeResult(ChargeOutcome Outcome, Guid? PaymentId, string? CardBrand, string? CardLast4, string? DeclineCode, string? Message)
{
    public static ChargeResult Approved(Guid paymentId, string cardBrand, string cardLast4) =>
        new(ChargeOutcome.Approved, paymentId, cardBrand, cardLast4, null, null);

    public static ChargeResult Declined(Guid? paymentId, string declineCode, string message) =>
        new(ChargeOutcome.Declined, paymentId, null, null, declineCode, message);
}

public interface IPaymentClient
{
    /// <summary>
    /// Charges a card. Returns Approved or Declined. Invalid card details are thrown as a 400 error with the
    /// Payment service's field errors; an unavailable Payment service is thrown as a 502.
    /// </summary>
    Task<ChargeResult> ChargeAsync(string idempotencyKey, ChargeRequest request, CancellationToken cancellationToken = default);

    Task RefundAsync(Guid paymentId, decimal amount, string reason, CancellationToken cancellationToken = default);
}

public class PaymentClient(HttpClient httpClient) : IPaymentClient
{
    private const string ServiceName = "Payment";

    private record PaymentResponse(Guid Id, string CardBrand, string CardLast4);

    public async Task<ChargeResult> ChargeAsync(string idempotencyKey, ChargeRequest request, CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "payments")
        {
            Content = JsonContent.Create(new
            {
                request.OrderReference,
                request.Amount,
                Currency = "USD",
                request.CustomerEmail,
                request.Card
            })
        };
        message.Headers.Add("Idempotency-Key", idempotencyKey);

        using var response = await DownstreamCall.SendAsync(ServiceName, () => httpClient.SendAsync(message, cancellationToken));

        if (response.StatusCode == HttpStatusCode.PaymentRequired)
        {
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            return ChargeResult.Declined(
                problem.TryGetProperty("paymentId", out var id) && id.TryGetGuid(out var paymentId) ? paymentId : null,
                problem.TryGetProperty("declineCode", out var code) ? code.GetString() ?? "CARD_DECLINED" : "CARD_DECLINED",
                problem.TryGetProperty("detail", out var detail) ? detail.GetString() ?? "Your card was declined." : "Your card was declined.");
        }

        await response.EnsureSuccessOrThrowAsync(ServiceName, cancellationToken);
        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>(cancellationToken)
                      ?? throw new DownstreamServiceException(ServiceName, "empty response");
        return ChargeResult.Approved(payment.Id, payment.CardBrand, payment.CardLast4);
    }

    public async Task RefundAsync(Guid paymentId, decimal amount, string reason, CancellationToken cancellationToken = default)
    {
        using var response = await DownstreamCall.SendAsync(ServiceName, () =>
            httpClient.PostAsJsonAsync($"payments/{paymentId}/refunds", new { amount, reason }, cancellationToken));
        await response.EnsureSuccessOrThrowAsync(ServiceName, cancellationToken);
    }
}
