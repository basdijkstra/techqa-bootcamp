using NUnit.Framework;
using static RestAssured.Dsl;

namespace PixelVault.IntegrationTests.Exercises;

[TestFixture]
public class Exercises01 : CatalogApiTestBase
{
    /*
     * TODO: write a test that retrieves the game with id 1
     * by sending a GET request to {CatalogBaseUrl}/games/1
     * and verifies that the response status code is 200.
     */
    [Test]
    public void GetGame1_VerifyStatusCode_ShouldBe200()
    {
        
    }

    /*
     * TODO: write a test that retrieves the game with id 1
     * by sending a GET request to {CatalogBaseUrl}/games/1
     * and verifies that the response Content-Type is "application/json; charset=utf-8".
     */
    [Test]
    public void GetGame1_VerifyContentType_ShouldBeJson()
    {
        
    }

    /*
     * TODO: write a test that retrieves the game with id 1
     * by sending a GET request to {CatalogBaseUrl}/games/1
     * and uses Header() to verify that the value of the Server response header is "Kestrel".
     */
    [Test]
    public void GetGame1_VerifyServerHeader_ShouldBeKestrel()
    {
        
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
        
    }
}
