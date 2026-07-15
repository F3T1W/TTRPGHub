using System.Net;
using System.Net.Http.Json;

namespace TTRPGHub.API.Tests;

[Collection("Api")]
public class RulesEndpointsTests(ApiFactory factory)
{
    [Fact]
    public async Task GetSystems_AnonymousAccess_ReturnsOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/rules/systems");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateSystem_ThenCreateEntry_ThenGetDetail_ReturnsEntry()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var createSystem = await client.PostAsJsonAsync("/api/v1/rules/systems", new { Name = $"My System {Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, createSystem.StatusCode);
        var system = await createSystem.Content.ReadFromJsonAsync<CreateGameSystemResponseDto>();

        var createEntry = await client.PostAsJsonAsync($"/api/v1/rules/{system!.Slug}/Spell", new
        {
            Title = "Custom Fireball",
            Summary = "Deals fire damage",
            ContentMarkdown = "A ball of fire.",
            StatsJson = (string?)null,
            Tags = Array.Empty<string>()
        });
        Assert.Equal(HttpStatusCode.Created, createEntry.StatusCode);
        var entry = await createEntry.Content.ReadFromJsonAsync<CreateRuleEntryResponseDto>();

        var detail = await client.GetAsync($"/api/v1/rules/{system.Slug}/Spell/{entry!.Slug}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
    }

    [Fact]
    public async Task CreateSystem_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/rules/systems", new { Name = "My System" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetEntries_InvalidCategory_ReturnsUnprocessableEntity()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/rules/pf2e/NotARealCategory?page=1&pageSize=40");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetEntries_NonExistentSystem_ReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/rules/nonexistent-{Guid.NewGuid():N}/Spell?page=1&pageSize=40");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEntry_ByNonOwner_ReturnsForbidden()
    {
        var owner = await factory.CreateClient().AuthenticateAsync();
        var createSystem = await owner.PostAsJsonAsync("/api/v1/rules/systems", new { Name = $"My System {Guid.NewGuid():N}" });
        var system = await createSystem.Content.ReadFromJsonAsync<CreateGameSystemResponseDto>();
        var createEntry = await owner.PostAsJsonAsync($"/api/v1/rules/{system!.Slug}/Spell", new
        {
            Title = "Custom Fireball",
            Summary = (string?)null,
            ContentMarkdown = (string?)null,
            StatsJson = (string?)null,
            Tags = Array.Empty<string>()
        });
        var entry = await createEntry.Content.ReadFromJsonAsync<CreateRuleEntryResponseDto>();

        var stranger = await factory.CreateClient().AuthenticateAsync();
        var delete = await stranger.DeleteAsync($"/api/v1/rules/{system.Slug}/Spell/{entry!.Slug}");

        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    private sealed record CreateGameSystemResponseDto(Guid Id, string Slug, string Name);
    private sealed record CreateRuleEntryResponseDto(Guid Id, string Slug);
}
