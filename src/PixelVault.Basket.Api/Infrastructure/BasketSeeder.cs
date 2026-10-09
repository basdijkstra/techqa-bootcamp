using PixelVault.Basket.Domain;
using PixelVault.Shared.TestSupport;

namespace PixelVault.Basket.Infrastructure;

/// <summary>
/// Seeds the promo codes. Dates are relative to "now" so the same codes are valid, expired or upcoming
/// whenever the app is started. Baskets are not seeded.
/// </summary>
public class BasketSeeder(BasketDbContext db, TimeProvider timeProvider) : IDataSeeder
{
    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Database.EnsureCreatedAsync(cancellationToken))
        {
            await SeedAsync(cancellationToken);
        }
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureDeletedAsync(cancellationToken);
        await db.Database.EnsureCreatedAsync(cancellationToken);
        await SeedAsync(cancellationToken);
    }

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var today = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);

        db.PromoCodes.AddRange(
            new PromoCode
            {
                Code = "WELCOME10", Description = "10% off your order", Type = PromoCodeType.Percentage, Value = 10,
                ValidFrom = today.AddYears(-1)
            },
            new PromoCode
            {
                Code = "SAVE5", Description = "$5 off orders of $30 or more", Type = PromoCodeType.FixedAmount, Value = 5,
                MinimumSpend = 30, ValidFrom = today.AddMonths(-2), ValidUntil = today.AddMonths(2)
            },
            new PromoCode
            {
                Code = "BIGSPENDER", Description = "$20 off orders of $150 or more", Type = PromoCodeType.FixedAmount, Value = 20,
                MinimumSpend = 150, ValidFrom = today.AddMonths(-1)
            },
            new PromoCode
            {
                Code = "RPGFAN", Description = "20% off RPG games", Type = PromoCodeType.Percentage, Value = 20,
                RestrictedToCategory = "Rpg", ValidFrom = today.AddDays(-10), ValidUntil = today.AddDays(20)
            },
            new PromoCode
            {
                Code = "ONEUSE", Description = "15% off, one order only", Type = PromoCodeType.Percentage, Value = 15,
                MaxUses = 1, ValidFrom = today.AddDays(-30)
            },
            new PromoCode
            {
                Code = "SPRING25", Description = "25% off the spring sale", Type = PromoCodeType.Percentage, Value = 25,
                ValidFrom = today.AddMonths(-7), ValidUntil = today.AddMonths(-6)
            },
            new PromoCode
            {
                Code = "HOLIDAY30", Description = "30% off the holiday sale", Type = PromoCodeType.Percentage, Value = 30,
                ValidFrom = today.AddDays(30), ValidUntil = today.AddDays(60)
            },
            new PromoCode
            {
                Code = "STAFFONLY", Description = "Staff discount (retired)", Type = PromoCodeType.Percentage, Value = 50,
                ValidFrom = today.AddYears(-2), IsActive = false
            });

        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }
}
