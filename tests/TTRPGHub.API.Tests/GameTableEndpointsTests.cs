using System.Net;
using System.Net.Http.Json;

namespace TTRPGHub.API.Tests;

[Collection("Api")]
public class GameTableEndpointsTests(ApiFactory factory)
{
    private static async Task<Guid> CreateSessionAsync(HttpClient client)
    {
        var create = await client.PostAsJsonAsync("/api/sessions", new
        {
            Title = "Open table",
            Description = (string?)null,
            System = "pf2e",
            MaxPlayers = 4,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Format = "Online",
            Location = (string?)null
        });
        var body = await create.Content.ReadFromJsonAsync<CreateSessionResponseDto>();
        return body!.SessionId;
    }

    [Fact]
    public async Task SendChatMessage_ReturnsMessage()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);

        var response = await client.PostAsJsonAsync($"/api/table/{sessionId}/messages", new { Content = "Hello table!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SendChatMessage_ByNonParticipant_ReturnsUnauthorized()
    {
        var organizer = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(organizer);

        var stranger = await factory.CreateClient().AuthenticateAsync();
        var response = await stranger.PostAsJsonAsync($"/api/table/{sessionId}/messages", new { Content = "Intrusion" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RollDice_ReturnsRollResult()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);

        var response = await client.PostAsJsonAsync($"/api/table/{sessionId}/roll", new { Expression = "1d20", Dc = (int?)null, Label = (string?)null });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateScene_ByOrganizer_ReturnsCreated()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);

        var response = await client.PostAsJsonAsync($"/api/table/{sessionId}/scenes", new { Name = "The Dungeon" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetTableState_SessionNotInProgress_ReturnsUnprocessableEntity()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);

        var response = await client.GetAsync($"/api/table/{sessionId}/state");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetTableState_NonExistentSession_ReturnsNotFound()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var response = await client.GetAsync($"/api/table/{Guid.NewGuid()}/state");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record CreateSessionResponseDto(Guid SessionId, string Title);
}
