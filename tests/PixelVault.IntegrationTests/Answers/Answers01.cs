using NUnit.Framework;
using static RestAssured.Dsl;

namespace PixelVault.IntegrationTests.Answers;

[TestFixture]
public class Answers01 : CatalogApiTestBase
{
    /*
     * TODO: write a test that retrieves the game with id 1
     * by sending a GET request to {CatalogBaseUrl}/games/1
     * and verifies that the response status code is 200.
     */
    [Test]
    public void GetGame1_VerifyStatusCode_ShouldBe200()
    {
        Given()
            .When()
            .Get($"{CatalogBaseUrl}/games/1")
            .Then()
            .AssertThat()
            .StatusCode(200);
    }

    /*
     * TODO: write a test that retrieves the game with id 1
     * by sending a GET request to {CatalogBaseUrl}/games/1
     * and verifies that the response Content-Type is "application/json; charset=utf-8".
     */
    [Test]
    public void GetGame1_VerifyContentType_ShouldBeJson()
    {
        Given()
            .When()
            .Get($"{CatalogBaseUrl}/games/1")
            .Then()
            .AssertThat()
            .ContentType("application/json; charset=utf-8");
    }

    /*
     * TODO: write a test that retrieves the game with id 1
     * by sending a GET request to {CatalogBaseUrl}/games/1
     * and uses Header() to verify that the value of the Server response header is "Kestrel".
     */
    [Test]
    public void GetGame1_VerifyServerHeader_ShouldBeKestrel()
    {
        Given()
            .When()
            .Get($"{CatalogBaseUrl}/games/1")
            .Then()
            .AssertThat()
            .Header("Server", "Kestrel");
    }

    /*
     * TODO: write a test that retrieves the game with id 1     
     * by sending a GET request to {CatalogBaseUrl}/games/1
     * and verifies that its title is "Ashes of Valdren".
     *
     * Use the JsonPath expression '$.title' to extract the title
     * property of the game.
     */
    [Test]
    public void GetGame1_VerifyTitle_ShouldBeAshesOfValdren()
    {
        Given()
            .When()
            .Get($"{CatalogBaseUrl}/games/1")
            .Then()
            .AssertThat()
            .Body("$.title", NHamcrest.Is.EqualTo("Ashes of Valdren"));
    }

    /*
     * TODO: write a test that retrieves the list of categories
     * by sending a GET request to {CatalogBaseUrl}/categories
     * and verifies that the list contains the value 'Action'.
     *
     * Use the JsonPath expression '$[*].name' to extract the name
     * property of each category in the list.
     * 
     * You will need to use NHamcrest.Has.Item() and NHamcrest.Is.EqualTo() to
     * verify that the list contains the value 'Action'.
     */
    [Test]
    public void GetCategories_VerifyActionCategoryExists()
    {
        Given()
            .When()
            .Get($"{CatalogBaseUrl}/categories")
            .Then()
            .AssertThat()
            .Body("$[*].name", NHamcrest.Has.Item(NHamcrest.Is.EqualTo("Action")));
    }
}
