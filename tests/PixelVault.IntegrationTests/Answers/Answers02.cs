using NUnit.Framework;
using static RestAssured.Dsl;

namespace PixelVault.IntegrationTests.Answers;

[TestFixture]
public class Answers02 : CatalogApiTestBase
{
    /*
     * The three tests below call the same endpoint ({CatalogBaseUrl}/games/[gameId])
     * with a different game id, and verify the stock status of that game.
     * They only differ in the game id and the expected stock status.
     */
    [Test]
    public void GetGame6_VerifyStockStatus_ShouldBeOutOfStock()
    {
        Given()
            .PathParam("gameId", 6)
            .When()
            .Get($"{CatalogBaseUrl}/games/[gameId]")
            .Then()
            .AssertThat()
            .Body("$.stockStatus", NHamcrest.Is.EqualTo("OutOfStock"));
    }

    [Test]
    public void GetGame12_VerifyStockStatus_ShouldBeLowStock()
    {
        Given()
            .PathParam("gameId", 12)
            .When()
            .Get($"{CatalogBaseUrl}/games/[gameId]")
            .Then()
            .AssertThat()
            .Body("$.stockStatus", NHamcrest.Is.EqualTo("LowStock"));
    }

    [Test]
    public void GetGame20_VerifyStockStatus_ShouldBePreOrder()
    {
        Given()
            .PathParam("gameId", 20)
            .When()
            .Get($"{CatalogBaseUrl}/games/[gameId]")
            .Then()
            .AssertThat()
            .Body("$.stockStatus", NHamcrest.Is.EqualTo("PreOrder"));
    }

    /*
     * TODO: refactor the three tests above into a single parameterized test using [TestCase].
     * The test method should take the game id and the expected stock status as parameters.
     * Give every test case a readable name using TestName.
     */
    [TestCase(6, "OutOfStock", TestName = "Game with ID 6 is out of stock")]
    [TestCase(12, "LowStock", TestName = "Game with ID 12 has low stock")]
    [TestCase(20, "PreOrder", TestName = "Game with ID 20 is a pre-order")]
    public void CheckStockStatusForGame(int gameId, string expectedStockStatus)
    {
        Given()
            .PathParam("gameId", gameId)
            .When()
            .Get($"{CatalogBaseUrl}/games/[gameId]")
            .Then()
            .AssertThat()
            .Body("$.stockStatus", NHamcrest.Is.EqualTo(expectedStockStatus));
    }

    /*
     * TODO: refactor the three tests above into a single parameterized test using [TestCaseSource].
     * Create a private static method named GameIdAndExpectedStockStatus that returns an IEnumerable<TestCaseData>,
     * with one TestCaseData object per test case. Give every test case a readable name using SetName().
     */
    [Test, TestCaseSource(nameof(GameIdAndExpectedStockStatus))]
    public void CheckStockStatusForGame_UsingTestCaseSource(int gameId, string expectedStockStatus)
    {
        Given()
            .PathParam("gameId", gameId)
            .When()
            .Get($"{CatalogBaseUrl}/games/[gameId]")
            .Then()
            .AssertThat()
            .Body("$.stockStatus", NHamcrest.Is.EqualTo(expectedStockStatus));
    }

    private static IEnumerable<TestCaseData> GameIdAndExpectedStockStatus()
    {
        yield return new TestCaseData(6, "OutOfStock").SetName("Game with ID 6 is out of stock - using TestCaseSource");
        yield return new TestCaseData(12, "LowStock").SetName("Game with ID 12 has low stock - using TestCaseSource");
        yield return new TestCaseData(20, "PreOrder").SetName("Game with ID 20 is a pre-order - using TestCaseSource");
    }
}
