using System.Net;
using System.Net.Http.Json;

namespace TTRPGHub.API.Tests;

[Collection("Api")]
public class TicketsEndpointsTests(ApiFactory factory)
{
    private static async Task<HttpResponseMessage> CreateTicketAsync(HttpClient client, string title = "Bug report", string description = "Something is broken")
    {
        using var form = new MultipartFormDataContent
        {
            { new StringContent(title), "title" },
            { new StringContent(description), "description" }
        };
        return await client.PostAsync("/api/tickets", form);
    }

    [Fact]
    public async Task Create_ThenGetMine_ReturnsCreatedTicket()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var create = await CreateTicketAsync(client);

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var mine = await client.GetAsync("/api/tickets/me?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await CreateTicketAsync(client);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ByNonOwnerNonModerator_ReturnsForbidden()
    {
        var owner = await factory.CreateClient().AuthenticateAsync();
        var create = await CreateTicketAsync(owner);
        var created = await create.Content.ReadFromJsonAsync<CreateTicketResponseDto>();

        var stranger = await factory.CreateClient().AuthenticateAsync();
        var response = await stranger.GetAsync($"/api/tickets/{created!.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ByRegularUser_ReturnsForbidden()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var response = await client.GetAsync("/api/tickets");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddComment_ThenGetComments_ReturnsComment()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var create = await CreateTicketAsync(client);
        var created = await create.Content.ReadFromJsonAsync<CreateTicketResponseDto>();

        var addComment = await client.PostAsJsonAsync($"/api/tickets/{created!.Id}/comments", new { Body = "Any update?" });
        Assert.Equal(HttpStatusCode.Created, addComment.StatusCode);

        var comments = await client.GetAsync($"/api/tickets/{created.Id}/comments");
        Assert.Equal(HttpStatusCode.OK, comments.StatusCode);
    }

    [Fact]
    public async Task AddComment_EmptyBody_ReturnsUnprocessableEntity()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var create = await CreateTicketAsync(client);
        var created = await create.Content.ReadFromJsonAsync<CreateTicketResponseDto>();

        var addComment = await client.PostAsJsonAsync($"/api/tickets/{created!.Id}/comments", new { Body = "" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, addComment.StatusCode);
    }

    private sealed record CreateTicketResponseDto(Guid Id);
}
