using PixelVault.Payment.Domain;

namespace PixelVault.Payment.Services;

public record FraudCheckInput(decimal Amount, string? CustomerEmail, IReadOnlyCollection<PaymentTransaction> PreviousAttemptsWithCard, DateTimeOffset Now);

public record FraudAssessment(int Score, bool IsBlocked, IReadOnlyList<string> Reasons);

public interface IFraudScorer
{
    FraudAssessment Assess(FraudCheckInput input);
}

/// <summary>
/// Adds up points for risky signals. A payment with a score of 70 or more is declined.
/// <code>
/// Amount of $1,000.00 or more                                      +40
/// Amount of $500.00 up to $999.99                                  +20
/// 3 or more attempts with the same card in the last 10 minutes     +40
/// 2 or more declined attempts with the same card in the last 24h   +30
/// Email address at a disposable email domain                       +25
/// </code>
/// </summary>
public class FraudScorer : IFraudScorer
{
    public const int BlockThreshold = 70;

    public static readonly IReadOnlySet<string> DisposableEmailDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "mailinator.com", "10minutemail.com", "guerrillamail.com", "tempmail.com", "trashmail.com"
    };

    public FraudAssessment Assess(FraudCheckInput input)
    {
        var score = 0;
        var reasons = new List<string>();

        if (input.Amount >= 1000m)
        {
            score += 40;
            reasons.Add("Amount of $1,000 or more");
        }
        else if (input.Amount >= 500m)
        {
            score += 20;
            reasons.Add("Amount of $500 or more");
        }

        var recentAttempts = input.PreviousAttemptsWithCard.Count(p => p.CreatedAt > input.Now.AddMinutes(-10));
        if (recentAttempts >= 3)
        {
            score += 40;
            reasons.Add($"{recentAttempts} attempts with this card in the last 10 minutes");
        }

        var recentDeclines = input.PreviousAttemptsWithCard.Count(p =>
            p.Status == PaymentStatus.Declined && p.CreatedAt > input.Now.AddHours(-24));
        if (recentDeclines >= 2)
        {
            score += 30;
            reasons.Add($"{recentDeclines} declined attempts with this card in the last 24 hours");
        }

        var domain = input.CustomerEmail?.Split('@').LastOrDefault();
        if (domain is not null && DisposableEmailDomains.Contains(domain))
        {
            score += 25;
            reasons.Add("Disposable email address");
        }

        return new FraudAssessment(score, score >= BlockThreshold, reasons);
    }
}
