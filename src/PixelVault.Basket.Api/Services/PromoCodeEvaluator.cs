using PixelVault.Basket.Domain;
using PixelVault.Shared.Web;

namespace PixelVault.Basket.Services;

public enum PromoCodeFailureReason
{
    NotFound,
    Inactive,
    NotYetValid,
    Expired,
    UsageLimitReached,
    NoEligibleItems,
    MinimumSpendNotMet
}

public record PromoEvaluation(bool IsValid, decimal Discount, PromoCodeFailureReason? FailureReason, string Message)
{
    public static PromoEvaluation Valid(decimal discount, string message) => new(true, discount, null, message);

    public static PromoEvaluation Invalid(PromoCodeFailureReason reason, string message) => new(false, 0m, reason, message);
}

public interface IPromoCodeEvaluator
{
    /// <summary>Checks whether a promo code can be used for these lines and works out the discount.</summary>
    PromoEvaluation Evaluate(PromoCode? promoCode, IReadOnlyCollection<PricedLine> lines, DateTimeOffset now);
}

/// <summary>
/// Promo code rules, checked in this order (the first one that fails is reported):
/// <list type="number">
/// <item>The code must exist and be active.</item>
/// <item><c>now</c> must be on or after <see cref="PromoCode.ValidFrom"/> and before <see cref="PromoCode.ValidUntil"/>.</item>
/// <item>The code must not have reached its maximum number of uses.</item>
/// <item>At least one line must be eligible (right category, if the code is restricted to one).</item>
/// <item>The basket subtotal (all lines) must be at least the minimum spend.</item>
/// </list>
/// The discount is calculated on the eligible lines only. A fixed amount is capped at the eligible total.
/// </summary>
public class PromoCodeEvaluator : IPromoCodeEvaluator
{
    public PromoEvaluation Evaluate(PromoCode? promoCode, IReadOnlyCollection<PricedLine> lines, DateTimeOffset now)
    {
        if (promoCode is null)
        {
            return PromoEvaluation.Invalid(PromoCodeFailureReason.NotFound, "This promo code does not exist.");
        }

        if (!promoCode.IsActive)
        {
            return PromoEvaluation.Invalid(PromoCodeFailureReason.Inactive, "This promo code is no longer available.");
        }

        if (now < promoCode.ValidFrom)
        {
            return PromoEvaluation.Invalid(PromoCodeFailureReason.NotYetValid,
                $"This promo code can be used from {promoCode.ValidFrom:MMMM d, yyyy}.");
        }

        if (promoCode.ValidUntil is { } validUntil && now >= validUntil)
        {
            return PromoEvaluation.Invalid(PromoCodeFailureReason.Expired, "This promo code has expired.");
        }

        if (promoCode.MaxUses is { } maxUses && promoCode.TimesUsed >= maxUses)
        {
            return PromoEvaluation.Invalid(PromoCodeFailureReason.UsageLimitReached, "This promo code has already been used the maximum number of times.");
        }

        var eligible = lines
            .Where(l => promoCode.RestrictedToCategory is null
                        || string.Equals(l.Category, promoCode.RestrictedToCategory, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (eligible.Count == 0)
        {
            return PromoEvaluation.Invalid(PromoCodeFailureReason.NoEligibleItems,
                promoCode.RestrictedToCategory is { } category
                    ? $"This promo code only applies to {CategoryDisplayName(category)} games."
                    : "Add a game to your basket to use this promo code.");
        }

        var subtotal = lines.Sum(l => l.LineTotal);
        if (promoCode.MinimumSpend is { } minimumSpend && subtotal < minimumSpend)
        {
            return PromoEvaluation.Invalid(PromoCodeFailureReason.MinimumSpendNotMet,
                $"Spend at least {minimumSpend:C} to use this promo code. You need {minimumSpend - subtotal:C} more.");
        }

        var eligibleTotal = eligible.Sum(l => l.LineTotal);
        var discount = promoCode.Type switch
        {
            PromoCodeType.Percentage => MoneyMath.RoundToCents(eligibleTotal * promoCode.Value / 100m),
            PromoCodeType.FixedAmount => Math.Min(promoCode.Value, eligibleTotal),
            _ => 0m
        };

        return PromoEvaluation.Valid(discount, $"{promoCode.Description} applied.");
    }

    private static string CategoryDisplayName(string category) =>
        category.Equals("Rpg", StringComparison.OrdinalIgnoreCase) ? "RPG" : category;
}
