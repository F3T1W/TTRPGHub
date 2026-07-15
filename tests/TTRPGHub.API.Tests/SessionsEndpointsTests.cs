using System.Net;
using System.Net.Http.Json;

namespace TTRPGHub.API.Tests;

[Collection("Api")]
public class SessionsEndpointsTests(ApiFactory factory)
{
    [Fact]
    public async Task Create_ThenGetDetail_ReturnsSession()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var create = await client.PostAsJsonAsync("/api/sessions", new
        {
            Title = "Open table",
            Description = "Bring a level 1 character",
            System = "pf2e",
            MaxPlayers = 4,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Format = "Online",
            Location = (string?)null
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<CreateSessionResponseDto>();

        var detail = await client.GetAsync($"/api/sessions/{created!.SessionId}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
    }

    [Fact]
    public async Task Create_MaxPlayersZero_ReturnsUnprocessableEntity()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var create = await client.PostAsJsonAsync("/api/sessions", new
        {
            Title = "Open table",
            Description = (string?)null,
            System = "pf2e",
            MaxPlayers = 0,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Format = "Online",
            Location = (string?)null
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, create.StatusCode);
    }

    [Fact]
    public async Task GetUpcoming_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/sessions/upcoming");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDetail_NonExistentId_ReturnsNotFound()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var response = await client.GetAsync($"/api/sessions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record CreateSessionResponseDto(Guid SessionId, string Title);
}
