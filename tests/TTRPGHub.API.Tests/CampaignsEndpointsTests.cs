using System.Net;
using System.Net.Http.Json;

namespace TTRPGHub.API.Tests;

[Collection("Api")]
public class CampaignsEndpointsTests(ApiFactory factory)
{
    [Fact]
    public async Task Create_ThenGetDetail_ReturnsCampaign()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var create = await client.PostAsJsonAsync("/api/v1/campaigns", new
        {
            Title = "The Beginning",
            Description = "A grand adventure",
            System = "pf2e"
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<CreateCampaignResponseDto>();

        var detail = await client.GetAsync($"/api/v1/campaigns/{created!.CampaignId}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/campaigns");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMine_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/campaigns/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDetail_NonExistentId_ReturnsNotFound()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var response = await client.GetAsync($"/api/v1/campaigns/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record CreateCampaignResponseDto(Guid CampaignId, string Title);
}
