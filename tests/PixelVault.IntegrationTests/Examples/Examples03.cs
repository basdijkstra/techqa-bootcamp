using NUnit.Framework;
using PixelVault.IntegrationTests.Examples.Models;
using static RestAssured.Dsl;

namespace PixelVault.IntegrationTests.Examples;

public class Examples03
{
    [Test]
    public void SerializePostObjectToJson()
    {
        Post post = new Post
        {
            UserId = 1,
            Title = "My new blog post",
            Body = "This is an awesome piece of content"
        };

        Given()
            .Body(post)
            .When()
            .Post("https://jsonplaceholder.typicode.com/posts")
            .Then()
            .StatusCode(201);
    }

    [Test]
    public void SerializeAnonymousObjectToJson()
    {
        var post = new
        {
            userId = 1,
            title = "My new blog post",
            body = "This is an awesome piece of content"
        };

        Given()
            .Body(post)
            .When()
            .Post("https://jsonplaceholder.typicode.com/posts")
            .Then()
            .StatusCode(201);
    }

    [Test]
    public void DeserializeJsonToPost()
    {
        var post = Given()
            .When()
            .Get("https://jsonplaceholder.typicode.com/posts/1")
            .DeserializeTo<Post>();

        Assert.That(post!.Title, Contains.Substring("sunt aut facere"));
    }

    [Test]
    public void DeserializeJsonToPostAfterVerification()
    {
        var post = Given()
            .When()
            .Get("https://jsonplaceholder.typicode.com/posts/1")
            .Then()
            .StatusCode(200)
            .DeserializeTo<Post>();

        Assert.That(post!.Title, Contains.Substring("sunt aut facere"));
    }
}