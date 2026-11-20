using NUnit.Framework;
using RestAssured.Logging;
using static RestAssured.Dsl;

namespace PixelVault.IntegrationTests.Examples;

public class Examples02
{
    [Test]
    public void UseQueryParameter()
    {
        Given()
            .QueryParam("text", "testcase")
            .When()
            .Get("http://md5.jsontest.com")
            .Then()
            .Body("$.md5", NHamcrest.Is.EqualTo("7489a25fc99976f06fecb807991c61cf"));
    }

    [Test]
    public void UsePathParameter()
    {
        Given()
            .PathParam("userId", 1)
            .When()
            .Get("https://jsonplaceholder.typicode.com/users/[userId]")
            .Then()
            .Body("$.name", NHamcrest.Is.EqualTo("Leanne Graham"));
    }

    [TestCase(1, "Leanne Graham", TestName = "User with ID 1 is Leanne Graham")]
    [TestCase(2, "Ervin Howell", TestName = "User with ID 2 is Ervin Howell")]
    [TestCase(3, "Clementine Bauch", TestName = "User with ID 3 is Clementine Bauch")]
    public void CheckNameForUser(int userId, string expectedUserName)
    {
        Given()
            .PathParam("userId", userId)
            .When()
            .Get("https://jsonplaceholder.typicode.com/users/[userId]")
            .Then()
            .Body("$.name", NHamcrest.Is.EqualTo(expectedUserName));
    }

    [Test, TestCaseSource(nameof(UserIdAndExpectedUserName))]
    public void CheckNameForUser_UsingTestCaseSource(int userId, string expectedUserName)
    {
        Given()
            .PathParam("userId", userId)
            .When()
            .Get("https://jsonplaceholder.typicode.com/users/[userId]")
            .Then()
            .Body("$.name", NHamcrest.Is.EqualTo(expectedUserName));
    }

    private static IEnumerable<TestCaseData> UserIdAndExpectedUserName()
    {
        yield return new TestCaseData(1, "Leanne Graham").SetName("User with ID 1 is Leanne Graham - using TestCaseSource");
        yield return new TestCaseData(2, "Ervin Howell").SetName("User with ID 2 is Ervin Howell - using TestCaseSource");
        yield return new TestCaseData(3, "Clementine Bauch").SetName("User with ID 3 is Clementine Bauch - using TestCaseSource");
    }
}