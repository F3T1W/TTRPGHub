using System.Net;
using System.Net.Http.Json;

namespace TTRPGHub.API.Tests;

[Collection("Api")]
public class EncountersEndpointsTests(ApiFactory factory)
{
    private static async Task<Guid> CreateCampaignAsync(HttpClient client)
    {
        var create = await client.PostAsJsonAsync("/api/v1/campaigns", new
        {
            Title = "Test Campaign",
            Description = (string?)null,
            System = "pf2e"
        });
        var body = await create.Content.ReadFromJsonAsync<CreateCampaignResponseDto>();
        return body!.CampaignId;
    }

    [Fact]
    public async Task Create_ThenGetDetail_ReturnsEncounter()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var campaignId = await CreateCampaignAsync(client);

        var create = await client.PostAsJsonAsync("/api/v1/encounters", new
        {
            CampaignId = campaignId,
            Title = "Goblin Ambush",
            Description = (string?)null,
            Difficulty = "Medium",
            Notes = (string?)null,
            Entries = new[] { new { Name = "Goblin Warrior", Count = 3, Notes = (string?)null } }
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<CreateEncounterResponseDto>();

        var detail = await client.GetAsync($"/api/v1/encounters/{created!.EncounterId}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
    }

    [Fact]
    public async Task Create_NonExistentCampaign_ReturnsNotFound()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var create = await client.PostAsJsonAsync("/api/v1/encounters", new
        {
            CampaignId = Guid.NewGuid(),
            Title = "Goblin Ambush",
            Description = (string?)null,
            Difficulty = "Medium",
            Notes = (string?)null,
            Entries = Array.Empty<object>()
        });

        Assert.Equal(HttpStatusCode.NotFound, create.StatusCode);
    }

    [Fact]
    public async Task Create_ByNonParticipant_ReturnsUnauthorized()
    {
        var owner = await factory.CreateClient().AuthenticateAsync();
        var campaignId = await CreateCampaignAsync(owner);

        var stranger = await factory.CreateClient().AuthenticateAsync();
        var create = await stranger.PostAsJsonAsync("/api/v1/encounters", new
        {
            CampaignId = campaignId,
            Title = "Goblin Ambush",
            Description = (string?)null,
            Difficulty = "Medium",
            Notes = (string?)null,
            Entries = Array.Empty<object>()
        });

        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }

    [Fact]
    public async Task Delete_ThenGetDetail_ReturnsNotFound()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var campaignId = await CreateCampaignAsync(client);

        var create = await client.PostAsJsonAsync("/api/v1/encounters", new
        {
            CampaignId = campaignId,
            Title = "To Delete",
            Description = (string?)null,
            Difficulty = "Easy",
            Notes = (string?)null,
            Entries = Array.Empty<object>()
        });
        var created = await create.Content.ReadFromJsonAsync<CreateEncounterResponseDto>();

        var delete = await client.DeleteAsync($"/api/v1/encounters/{created!.EncounterId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var detail = await client.GetAsync($"/api/v1/encounters/{created.EncounterId}");
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
    }

    private sealed record CreateCampaignResponseDto(Guid CampaignId, string Title);
    private sealed record CreateEncounterResponseDto(Guid EncounterId);
}
