using System.Net;
using System.Net.Http.Json;

namespace TTRPGHub.API.Tests;

[Collection("Api")]
public class HomebrewEndpointsTests(ApiFactory factory)
{
    [Fact]
    public async Task Create_ThenGetDetail_ReturnsHomebrewItem()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var create = await client.PostAsJsonAsync("/api/v1/homebrew", new
        {
            Title = "Fireball+",
            Description = "A stronger fireball",
            System = "pf2e",
            Type = "Spell",
            Content = "Deals extra damage.",
            Tags = "fire,evocation"
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = await create.Content.ReadFromJsonAsync<Guid>();

        var detail = await client.GetAsync($"/api/v1/homebrew/{id}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/v1/homebrew", new
        {
            Title = "Fireball+",
            Description = "A stronger fireball",
            System = "pf2e",
            Type = "Spell",
            Content = "Deals extra damage.",
            Tags = "fire,evocation"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }

    [Fact]
    public async Task Create_EmptyTitle_ReturnsUnprocessableEntity()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var create = await client.PostAsJsonAsync("/api/v1/homebrew", new
        {
            Title = "",
            Description = "A stronger fireball",
            System = "pf2e",
            Type = "Spell",
            Content = "Deals extra damage.",
            Tags = ""
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, create.StatusCode);
    }

    [Fact]
    public async Task Search_AnonymousAccess_ReturnsOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/homebrew?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetDetail_NonExistentId_ReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/homebrew/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ByNonOwner_ReturnsForbidden()
    {
        var owner = await factory.CreateClient().AuthenticateAsync();
        var create = await owner.PostAsJsonAsync("/api/v1/homebrew", new
        {
            Title = "Fireball+",
            Description = "A stronger fireball",
            System = "pf2e",
            Type = "Spell",
            Content = "Deals extra damage.",
            Tags = ""
        });
        var id = await create.Content.ReadFromJsonAsync<Guid>();

        var stranger = await factory.CreateClient().AuthenticateAsync();
        var delete = await stranger.DeleteAsync($"/api/v1/homebrew/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }
}
