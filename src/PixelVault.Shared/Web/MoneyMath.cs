namespace PixelVault.Shared.Web;

public static class MoneyMath
{
    /// <summary>Rounds an amount to whole cents, with halves rounded away from zero ($0.125 becomes $0.13).</summary>
    public static decimal RoundToCents(decimal amount) =>
        Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
