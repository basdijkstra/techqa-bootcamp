using NUnit.Framework;
using static RestAssured.Dsl;

namespace PixelVault.IntegrationTests.Exercises;

[TestFixture]
public class Exercises02 : CatalogApiTestBase
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


    /*
     * TODO: refactor the three tests above into a single parameterized test using [TestCaseSource].
     * Create a private static method named GameIdAndExpectedStockStatus that returns an IEnumerable<TestCaseData>,
     * with one TestCaseData object per test case. Give every test case a readable name using SetName().
     */

}
