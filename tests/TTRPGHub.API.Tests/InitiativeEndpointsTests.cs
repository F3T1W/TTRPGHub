using System.Net;
using System.Net.Http.Json;

namespace TTRPGHub.API.Tests;

[Collection("Api")]
public class InitiativeEndpointsTests(ApiFactory factory)
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
    public async Task Create_ThenGetDetail_ReturnsTracker()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var campaignId = await CreateCampaignAsync(client);

        var create = await client.PostAsJsonAsync("/api/v1/trackers", new { CampaignId = campaignId, Name = "Boss fight" });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<CreateTrackerResponseDto>();

        var detail = await client.GetAsync($"/api/v1/trackers/{created!.TrackerId}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
    }

    [Fact]
    public async Task Create_ByNonOrganizer_ReturnsUnauthorized()
    {
        var owner = await factory.CreateClient().AuthenticateAsync();
        var campaignId = await CreateCampaignAsync(owner);

        var stranger = await factory.CreateClient().AuthenticateAsync();
        var create = await stranger.PostAsJsonAsync("/api/v1/trackers", new { CampaignId = campaignId, Name = "Boss fight" });

        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }

    [Fact]
    public async Task SetEntries_ThenStart_ReturnsNoContent()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var campaignId = await CreateCampaignAsync(client);
        var create = await client.PostAsJsonAsync("/api/v1/trackers", new { CampaignId = campaignId, Name = "Boss fight" });
        var created = await create.Content.ReadFromJsonAsync<CreateTrackerResponseDto>();

        var setEntries = await client.PostAsJsonAsync($"/api/v1/trackers/{created!.TrackerId}/entries", new[]
        {
            new { Name = "Hero", Initiative = 15, MaxHp = 20, CurrentHp = 20, ArmorClass = 16, IsPlayerCharacter = true, Notes = (string?)null }
        });
        Assert.Equal(HttpStatusCode.NoContent, setEntries.StatusCode);

        var start = await client.PostAsync($"/api/v1/trackers/{created.TrackerId}/start", null);
        Assert.Equal(HttpStatusCode.NoContent, start.StatusCode);
    }

    [Fact]
    public async Task GetDetail_NonExistentId_ReturnsNotFound()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var response = await client.GetAsync($"/api/v1/trackers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ThenGetDetail_ReturnsNotFound()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var campaignId = await CreateCampaignAsync(client);
        var create = await client.PostAsJsonAsync("/api/v1/trackers", new { CampaignId = campaignId, Name = "To Delete" });
        var created = await create.Content.ReadFromJsonAsync<CreateTrackerResponseDto>();

        var delete = await client.DeleteAsync($"/api/v1/trackers/{created!.TrackerId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var detail = await client.GetAsync($"/api/v1/trackers/{created.TrackerId}");
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
    }

    private sealed record CreateCampaignResponseDto(Guid CampaignId, string Title);
    private sealed record CreateTrackerResponseDto(Guid TrackerId);
}
