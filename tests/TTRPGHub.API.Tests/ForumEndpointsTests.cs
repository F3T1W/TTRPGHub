using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using TTRPGHub.Entities.Forum;

namespace TTRPGHub.API.Tests;

[Collection("Api")]
public class ForumEndpointsTests(ApiFactory factory)
{
    private async Task<Guid> SeedCategoryAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = ForumCategory.Create($"Category {Guid.NewGuid():N}", "desc", $"cat-{Guid.NewGuid():N}");
        db.ForumCategories.Add(category);
        await db.SaveChangesAsync();
        return category.Id.Value;
    }

    [Fact]
    public async Task GetCategories_AnonymousAccess_ReturnsOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/forum/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateTopic_ThenGetPosts_ReturnsFirstPost()
    {
        var categoryId = await SeedCategoryAsync();
        var client = await factory.CreateClient().AuthenticateAsync();

        var create = await client.PostAsJsonAsync("/api/v1/forum/topics", new
        {
            CategoryId = categoryId,
            Title = "Hello world",
            FirstPostContent = "This is my first topic."
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var topicId = await create.Content.ReadFromJsonAsync<Guid>();

        var posts = await client.GetAsync($"/api/v1/forum/topics/{topicId}/posts?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, posts.StatusCode);
    }

    [Fact]
    public async Task CreateTopic_NonExistentCategory_ReturnsNotFound()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var create = await client.PostAsJsonAsync("/api/v1/forum/topics", new
        {
            CategoryId = Guid.NewGuid(),
            Title = "Hello world",
            FirstPostContent = "This is my first topic."
        });

        Assert.Equal(HttpStatusCode.NotFound, create.StatusCode);
    }

    [Fact]
    public async Task CreateTopic_WithoutAuth_ReturnsUnauthorized()
    {
        var categoryId = await SeedCategoryAsync();
        var client = factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/v1/forum/topics", new
        {
            CategoryId = categoryId,
            Title = "Hello world",
            FirstPostContent = "This is my first topic."
        });

        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }

    [Fact]
    public async Task PinTopic_WithoutModeratorRole_ReturnsForbidden()
    {
        var categoryId = await SeedCategoryAsync();
        var client = await factory.CreateClient().AuthenticateAsync();
        var create = await client.PostAsJsonAsync("/api/v1/forum/topics", new
        {
            CategoryId = categoryId,
            Title = "Hello world",
            FirstPostContent = "This is my first topic."
        });
        var topicId = await create.Content.ReadFromJsonAsync<Guid>();

        var pin = await client.PutAsJsonAsync($"/api/v1/forum/topics/{topicId}/pin", new { Pinned = true });

        Assert.Equal(HttpStatusCode.Forbidden, pin.StatusCode);
    }
}
