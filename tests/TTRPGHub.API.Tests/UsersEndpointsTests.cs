using System.Net;
using System.Net.Http.Json;

namespace TTRPGHub.API.Tests;

[Collection("Api")]
public class UsersEndpointsTests(ApiFactory factory)
{
    [Fact]
    public async Task GetProfile_AnonymousAccess_ReturnsOk()
    {
        var client = factory.CreateClient();
        var userId = await factory.CreateClient().AuthenticateWithIdAsync();

        var response = await client.GetAsync($"/api/v1/users/{userId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_NonExistentUser_ReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ThenGetProfile_ReflectsChange()
    {
        var client = factory.CreateClient();
        var userId = await client.AuthenticateWithIdAsync();

        var update = await client.PutAsJsonAsync("/api/v1/users/me/profile", new
        {
            DisplayName = "Grog the Strong",
            Bio = "I hit things.",
            City = "Whitestone"
        });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var profile = await factory.CreateClient().GetAsync($"/api/v1/users/{userId}");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/v1/users/me/profile", new
        {
            DisplayName = "Grog",
            Bio = (string?)null,
            City = (string?)null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminUsers_ByRegularUser_ReturnsForbidden()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var response = await client.GetAsync("/api/v1/users/admin?page=1&pageSize=30");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ChangeUserRole_ByRegularUser_ReturnsForbidden()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var response = await client.PatchAsJsonAsync($"/api/v1/users/admin/{Guid.NewGuid()}/role", new { Role = "Admin" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
