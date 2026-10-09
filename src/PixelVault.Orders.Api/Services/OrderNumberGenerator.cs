using System.Security.Cryptography;

namespace PixelVault.Orders.Services;

public interface IOrderNumberGenerator
{
    string Generate(DateTimeOffset now);
}

/// <summary>
/// Order numbers look like <c>PV-20261009-K7M2Q</c>: the UTC date plus 5 random characters. The letters I and O
/// and the digits 0 and 1 are left out, because they are easy to confuse when read out over the phone.
/// </summary>
public class OrderNumberGenerator : IOrderNumberGenerator
{
    public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    public const int SuffixLength = 5;

    public string Generate(DateTimeOffset now)
    {
        var suffix = RandomNumberGenerator.GetString(Alphabet, SuffixLength);
        return $"PV-{now.UtcDateTime:yyyyMMdd}-{suffix}";
    }
}
