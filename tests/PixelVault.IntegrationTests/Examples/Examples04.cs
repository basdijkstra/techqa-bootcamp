using NUnit.Framework;
using static RestAssured.Dsl;

namespace PixelVault.IntegrationTests.Examples;

public class Examples04
{
    [Test]
    public void CaptureAndReuseUniqueId()
    {
        string userId = (string)Given()
            .When()
            .Post("https://my.user.api/user")
            .Then()
            .Extract()
            .Body("$.id");

        Given()
            .PathParam("userId", userId)
            .When()
            .Get("https://my.user.api/user/[userId]")
            .Then()
            .StatusCode(200);
    }
}