using System.Net;
using System.Net.Http.Json;

namespace TTRPGHub.API.Tests;

[Collection("Api")]
public class EventsEndpointsTests(ApiFactory factory)
{
    private static object NewEventPayload(int maxParticipants = 4) => new
    {
        Title = "Open table",
        Description = "Bring a level 1 character",
        System = "pf2e",
        Format = "Online",
        Location = (string?)null,
        OnlineLink = "https://discord.gg/test",
        StartsAt = DateTime.UtcNow.AddDays(1),
        MaxParticipants = maxParticipants
    };

    [Fact]
    public async Task Create_ThenGetDetail_ReturnsEvent()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var create = await client.PostAsJsonAsync("/api/v1/events", NewEventPayload());

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = await create.Content.ReadFromJsonAsync<Guid>();

        var detail = await client.GetAsync($"/api/v1/events/{id}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
    }

    [Fact]
    public async Task GetAll_AnonymousAccess_ReturnsOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/events?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/v1/events", NewEventPayload());

        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }

    [Fact]
    public async Task Create_PastDate_ReturnsUnprocessableEntity()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var create = await client.PostAsJsonAsync("/api/v1/events", new
        {
            Title = "Open table",
            Description = (string?)null,
            System = "pf2e",
            Format = "Online",
            Location = (string?)null,
            OnlineLink = (string?)null,
            StartsAt = DateTime.UtcNow.AddDays(-1),
            MaxParticipants = 4
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, create.StatusCode);
    }

    [Fact]
    public async Task Register_ThenRegisterAgain_ReturnsConflict()
    {
        var organizer = await factory.CreateClient().AuthenticateAsync();
        var create = await organizer.PostAsJsonAsync("/api/v1/events", NewEventPayload());
        var id = await create.Content.ReadFromJsonAsync<Guid>();

        var participant = await factory.CreateClient().AuthenticateAsync();
        var first = await participant.PostAsync($"/api/v1/events/{id}/register", null);
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var second = await participant.PostAsync($"/api/v1/events/{id}/register", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_NonExistentEvent_ReturnsNotFound()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var response = await client.PostAsync($"/api/v1/events/{Guid.NewGuid()}/register", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
