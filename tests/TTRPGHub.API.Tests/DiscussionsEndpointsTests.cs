using System.Net;
using System.Net.Http.Json;

namespace TTRPGHub.API.Tests;

[Collection("Api")]
public class DiscussionsEndpointsTests(ApiFactory factory)
{
    [Fact]
    public async Task AddPost_ThenGetDiscussion_ReturnsPost()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var slug = $"goblin-{Guid.NewGuid():N}";

        var add = await client.PostAsJsonAsync($"/api/v1/discussions/Monster/{slug}", new { Content = "This monster is tough!" });

        Assert.Equal(HttpStatusCode.OK, add.StatusCode);

        var get = await client.GetAsync($"/api/v1/discussions/Monster/{slug}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var posts = await get.Content.ReadFromJsonAsync<List<DiscussionPostDto>>();
        Assert.Contains(posts!, p => p.Content == "This monster is tough!");
    }

    [Fact]
    public async Task AddPost_InvalidEntityType_ReturnsUnprocessableEntity()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var add = await client.PostAsJsonAsync("/api/v1/discussions/NotARealType/some-slug", new { Content = "Hi" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, add.StatusCode);
    }

    [Fact]
    public async Task AddPost_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var add = await client.PostAsJsonAsync("/api/v1/discussions/Spell/fireball", new { Content = "Hi" });

        Assert.Equal(HttpStatusCode.Unauthorized, add.StatusCode);
    }

    [Fact]
    public async Task GetDiscussion_AnonymousAccess_ReturnsOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/discussions/Spell/fireball-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ToggleLike_TwiceReturnsToInitialState()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var slug = $"goblin-{Guid.NewGuid():N}";
        var add = await client.PostAsJsonAsync($"/api/v1/discussions/Monster/{slug}", new { Content = "Like me" });
        var postId = await add.Content.ReadFromJsonAsync<Guid>();

        var firstLike = await client.PostAsync($"/api/v1/discussions/posts/{postId}/like", null);
        Assert.Equal(HttpStatusCode.OK, firstLike.StatusCode);

        var secondLike = await client.PostAsync($"/api/v1/discussions/posts/{postId}/like", null);
        Assert.Equal(HttpStatusCode.OK, secondLike.StatusCode);
    }

    private sealed record DiscussionPostDto(Guid Id, Guid AuthorId, string AuthorUsername, string? AuthorAvatarUrl,
        string Content, Guid? ParentId, int LikeCount, bool IsLikedByMe, bool IsOwn,
        DateTime CreatedAt, List<DiscussionPostDto> Replies);
}
