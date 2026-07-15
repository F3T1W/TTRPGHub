using System.Net;
using System.Net.Http.Json;

namespace TTRPGHub.API.Tests;

[Collection("Api")]
public class ModerationEndpointsTests(ApiFactory factory)
{
    [Fact]
    public async Task CreateReport_ReturnsCreated()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var response = await client.PostAsJsonAsync("/api/v1/reports", new
        {
            EntityType = "ForumPost",
            EntityId = Guid.NewGuid(),
            Reason = "Spam content"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateReport_InvalidEntityType_ReturnsUnprocessableEntity()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var response = await client.PostAsJsonAsync("/api/v1/reports", new
        {
            EntityType = "NotARealType",
            EntityId = Guid.NewGuid(),
            Reason = "Spam content"
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task CreateReport_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/reports", new
        {
            EntityType = "ForumPost",
            EntityId = Guid.NewGuid(),
            Reason = "Spam content"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOpenReports_ByRegularUser_ReturnsForbidden()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var response = await client.GetAsync("/api/v1/reports");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ResolveReport_ByRegularUser_ReturnsForbidden()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var response = await client.PatchAsJsonAsync($"/api/v1/reports/{Guid.NewGuid()}/resolve", new { Status = "Resolved" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetModerationLog_ByRegularUser_ReturnsForbidden()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var response = await client.GetAsync("/api/v1/moderation-log");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
