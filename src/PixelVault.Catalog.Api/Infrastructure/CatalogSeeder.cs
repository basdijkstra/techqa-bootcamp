using PixelVault.Catalog.Domain;
using PixelVault.Shared.Seed;
using PixelVault.Shared.TestSupport;

namespace PixelVault.Catalog.Infrastructure;

/// <summary>
/// Seeds 20 fictional games. Dates for sales and releases are relative to "now", so that the same
/// situations exist whenever the app is started:
/// <list type="bullet">
/// <item>Every stock status: in stock, low stock (1, 3 and exactly 5 units), out of stock, pre-order.</item>
/// <item>Active sales, a sale that ends today, a sale that starts in 3 days, a sale that ended yesterday,
/// and a sale that hits the $4.99 price floor.</item>
/// <item>Every ESRB rating, including M (17+) and AO (18+) games for the age check at checkout.</item>
/// </list>
/// </summary>
public class CatalogSeeder(CatalogDbContext db, TimeProvider timeProvider) : IDataSeeder
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
        var games = CreateGames(now, today);
        db.Games.AddRange(games);
        await db.SaveChangesAsync(cancellationToken);

        db.ChangeTracker.Clear();
        db.StockAdjustments.AddRange(CreateAdjustmentHistory(games, now));
        db.StockReservations.AddRange(CreateReservationsForSeededOrders(now));
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    /// <summary>
    /// Committed reservations for the seeded orders, so cancelling one of them puts its stock back.
    /// The seeded stock levels are what is left after these orders.
    /// </summary>
    private static IEnumerable<StockReservation> CreateReservationsForSeededOrders(DateTimeOffset now) =>
        SeedOrders.All
            .Where(o => o.Status is not ("Cancelled" or "PaymentFailed"))
            .SelectMany(o => o.Lines.Select(l => new StockReservation
            {
                OrderReference = o.OrderNumber(now),
                GameId = l.GameId,
                Quantity = l.Quantity,
                ConsumesStock = !l.IsPreOrder,
                Status = ReservationStatus.Committed,
                CreatedAt = o.CreatedAt(now),
                UpdatedAt = o.CreatedAt(now)
            }));

    private static List<Game> CreateGames(DateTimeOffset now, DateTimeOffset today)
    {
        var todayDate = DateOnly.FromDateTime(today.UtcDateTime);

        return
        [
            Game(1, "Ashes of Valdren", Category.Rpg, Platform.PC | Platform.PlayStation | Platform.Xbox, AgeRating.M,
                new DateOnly(2025, 3, 14), 59.99m, 42, "Emberforge Studios", "Starlit Publishing", "#7a1f1f", "#f2a541",
                "Rebuild a fallen kingdom in a 60-hour open-world RPG where every choice leaves scars on the land."),

            Game(2, "Neon Drift 2088", Category.Racing, Platform.PC | Platform.PlayStation, AgeRating.E10Plus,
                new DateOnly(2024, 11, 8), 49.99m, 25, "Northlight Interactive", "Copperline Entertainment", "#1b1464", "#ff2fd0",
                "Arcade street racing through a rain-soaked megacity. Build your car, own the night.",
                sale: (25, now.AddDays(-3), today.AddDays(10))),

            Game(3, "Hollow Lantern: Uncut", Category.Horror, Platform.PC | Platform.PlayStation | Platform.Xbox, AgeRating.AO,
                new DateOnly(2025, 10, 31), 39.99m, 3, "Gloamworks", "Kestrel Interactive", "#0d0d0d", "#c9a227",
                "The uncensored edition of the survival horror hit. One lantern, one night, no way back."),

            Game(4, "Kingdoms of Brass", Category.Strategy, Platform.PC, AgeRating.T,
                new DateOnly(2023, 6, 20), 54.99m, 18, "Brassbound Games", "Starlit Publishing", "#5c3d1e", "#d4a373",
                "Grand strategy in a steam-powered empire. Trade, scheme and conquer across four continents.",
                sale: (20, today.AddDays(3), today.AddDays(10))),

            Game(5, "Pixel Peaks", Category.Platformer, Platform.Switch | Platform.PC, AgeRating.E,
                new DateOnly(2022, 4, 2), 19.99m, 60, "Tiny Summit", "Tiny Summit", "#3a86ff", "#ffbe0b",
                "A tough but fair climbing platformer with 300 hand-made levels and a lot of heart."),

            Game(6, "Starfall Tactics", Category.Strategy, Platform.PC | Platform.Switch, AgeRating.T,
                new DateOnly(2024, 2, 27), 44.99m, 0, "Orbital Forge", "Kestrel Interactive", "#03045e", "#90e0ef",
                "Turn-based squad tactics on a dying space station. Every crew member counts."),

            Game(7, "Goalrush 26", Category.Sports, Platform.PlayStation | Platform.Xbox | Platform.Switch, AgeRating.E,
                new DateOnly(2025, 9, 26), 69.99m, 35, "Pitchside Digital", "Copperline Entertainment", "#2d6a4f", "#ffffff",
                "The most complete soccer game yet, with 700 licensed-sounding clubs and a brand new career mode.",
                sale: (15, today.AddDays(-14), today.AddDays(-1))),

            Game(8, "The Clockmaker's Riddle", Category.Puzzle, Platform.PC | Platform.PlayStation | Platform.Xbox | Platform.Switch, AgeRating.E,
                new DateOnly(2023, 1, 17), 14.99m, 14, "Cogwheel Puzzles", "Cogwheel Puzzles", "#6b4226", "#e9c46a",
                "Solve the mechanical puzzles hidden in an old clock tower and uncover what happened to its maker."),

            Game(9, "Deepwater Station", Category.Horror, Platform.PC | Platform.PlayStation, AgeRating.M,
                new DateOnly(2024, 8, 15), 34.99m, 9, "Abyssal Interactive", "Kestrel Interactive", "#023e8a", "#00b4d8",
                "Something is wrong on the deepest research station in the Pacific. The pressure is rising.",
                sale: (40, now.AddDays(-1), today.AddDays(5))),

            Game(10, "Tidebound", Category.Adventure, Platform.PC | Platform.PlayStation | Platform.Xbox | Platform.Switch, AgeRating.E10Plus,
                new DateOnly(2025, 5, 9), 29.99m, 22, "Saltwind Games", "Starlit Publishing", "#0077b6", "#f4a261",
                "Sail a small boat across a flooded world, trade with island towns and chart the unknown."),

            Game(11, "Ironclad Legion", Category.Action, Platform.PlayStation | Platform.Xbox, AgeRating.T,
                new DateOnly(2024, 10, 1), 59.99m, 12, "Iron Rook", "Copperline Entertainment", "#495057", "#e63946",
                "Lead a squad of mech pilots in fast third-person battles across a war-torn frontier."),

            Game(12, "Farmstead Valley", Category.Simulation, Platform.PC | Platform.Switch, AgeRating.E,
                new DateOnly(2023, 9, 5), 24.99m, 1, "Meadowlark Studio", "Meadowlark Studio", "#606c38", "#fefae0",
                "Grow crops, raise animals and turn a neglected farm into the pride of the valley."),

            Game(13, "Shadow Protocol", Category.Action, Platform.PC | Platform.PlayStation | Platform.Xbox, AgeRating.M,
                new DateOnly(2025, 2, 21), 64.99m, 30, "Nightglass", "Kestrel Interactive", "#111111", "#8d99ae",
                "A stealth action thriller. Infiltrate, adapt, and never get seen."),

            Game(14, "Sky Courier", Category.Simulation, Platform.PC, AgeRating.E,
                new DateOnly(2024, 6, 12), 19.99m, 8, "Cloudhop Games", "Cloudhop Games", "#48cae4", "#ffffff",
                "Fly small cargo planes between mountain villages. Relaxing, until the weather turns.",
                sale: (30, today.AddDays(-6), today.AddDays(1))),

            Game(15, "Whispering Pines", Category.Adventure, Platform.PC | Platform.Switch, AgeRating.T,
                new DateOnly(2022, 10, 14), 22.99m, 6, "Tallpine Collective", "Starlit Publishing", "#1b4332", "#b7e4c7",
                "A narrative mystery in a small mountain town where nobody tells the whole truth."),

            Game(16, "Rally Kings: Gravel", Category.Racing, Platform.PlayStation | Platform.Xbox | Platform.PC, AgeRating.E,
                new DateOnly(2023, 11, 3), 39.99m, 0, "Dustline Motorsports", "Copperline Entertainment", "#9c6644", "#ffd166",
                "Off-road rally racing on 90 stages across five continents, with full damage modelling."),

            Game(17, "Mechanica Arena", Category.Action, Platform.PC, AgeRating.T,
                new DateOnly(2022, 7, 22), 9.99m, 50, "Rivet & Bolt", "Rivet & Bolt", "#ff6d00", "#2b2d42",
                "Build a robot from spare parts and smash your way to the top of the arena league.",
                sale: (60, today.AddDays(-2), today.AddDays(12))),

            Game(18, "Lumen Garden", Category.Puzzle, Platform.Switch, AgeRating.E,
                new DateOnly(2024, 3, 8), 12.99m, 5, "Glowmoss", "Glowmoss", "#7209b7", "#4cc9f0",
                "Guide light through a living garden in this calm puzzle game with 150 levels."),

            Game(19, "Court Legends Basketball", Category.Sports, Platform.PlayStation | Platform.Xbox, AgeRating.E,
                new DateOnly(2025, 9, 5), 54.99m, 20, "Hardwood Interactive", "Copperline Entertainment", "#e85d04", "#03071e",
                "Five-on-five street and arena basketball with a full franchise mode."),

            Game(20, "Echoes of the Void", Category.Rpg, Platform.PC | Platform.PlayStation, AgeRating.M,
                todayDate.AddDays(45), 69.99m, 0, "Voidlight Studios", "Starlit Publishing", "#240046", "#e0aaff",
                "A sci-fi RPG about the last crew of a lost colony ship. Pre-order now, ships on release day.")
        ];
    }

    private static Game Game(
        int rank, string title, Category category, Platform platforms, AgeRating rating, DateOnly releaseDate,
        decimal price, int stock, string developer, string publisher, string primaryColor, string secondaryColor,
        string description, (int Percentage, DateTimeOffset Starts, DateTimeOffset Ends)? sale = null) => new()
    {
        Slug = Slugify(title),
        Title = title,
        Description = description,
        Developer = developer,
        Publisher = publisher,
        Category = category,
        Platforms = platforms,
        AgeRating = rating,
        ReleaseDate = releaseDate,
        BasePrice = price,
        StockQuantity = stock,
        FeaturedRank = rank,
        CoverColorPrimary = primaryColor,
        CoverColorSecondary = secondaryColor,
        SalePercentage = sale?.Percentage,
        SaleStartsAt = sale?.Starts,
        SaleEndsAt = sale?.Ends
    };

    private static IEnumerable<StockAdjustment> CreateAdjustmentHistory(List<Game> games, DateTimeOffset now)
    {
        var byTitle = games.ToDictionary(g => g.Title);

        StockAdjustment Adjustment(string title, int change, int after, StockAdjustmentReason reason, string? note, string by, double daysAgo) => new()
        {
            GameId = byTitle[title].Id,
            QuantityChange = change,
            QuantityBefore = after - change,
            QuantityAfter = after,
            Reason = reason,
            Note = note,
            PerformedBy = by,
            PerformedAt = now.AddDays(-daysAgo)
        };

        return
        [
            Adjustment("Ashes of Valdren", 40, 45, StockAdjustmentReason.Restock, "Delivery from distributor", "admin", 20),
            Adjustment("Ashes of Valdren", -3, 42, StockAdjustmentReason.Damaged, "Cracked cases", "admin", 12),
            Adjustment("Starfall Tactics", -2, 0, StockAdjustmentReason.Correction, "Stock count found 2 fewer than system", "admin", 9),
            Adjustment("Farmstead Valley", -1, 1, StockAdjustmentReason.Lost, null, "admin", 4),
            Adjustment("Goalrush 26", 30, 35, StockAdjustmentReason.Restock, "Launch stock", "admin", 15),
            Adjustment("Lumen Garden", 1, 5, StockAdjustmentReason.Return, "Unopened return", "admin", 2)
        ];
    }

    private static string Slugify(string title)
    {
        var chars = title.ToLowerInvariant()
            .Where(c => c != '\'')
            .Select(c => char.IsLetterOrDigit(c) ? c : ' ')
            .ToArray();
        return string.Join('-', new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
