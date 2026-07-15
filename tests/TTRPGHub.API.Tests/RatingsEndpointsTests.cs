using System.Net;
using System.Net.Http.Json;

namespace TTRPGHub.API.Tests;

[Collection("Api")]
public class RatingsEndpointsTests(ApiFactory factory)
{
    private static async Task<Guid> RegisterUserAsync(HttpClient client)
    {
        var email = $"user-{Guid.NewGuid():N}@test.local";
        var username = $"user{Guid.NewGuid():N}"[..12];
        var register = await client.PostAsJsonAsync("/api/auth/register", new { Username = username, Email = email, Password = "Sup3rSecret!" });
        var body = await register.Content.ReadFromJsonAsync<RegisterResponseDto>();
        return body!.UserId;
    }

    [Fact]
    public async Task RateUser_ThenGetRatings_ReturnsRating()
    {
        var rater = await factory.CreateClient().AuthenticateAsync();
        var rateeId = await RegisterUserAsync(factory.CreateClient());

        var rate = await rater.PostAsJsonAsync($"/api/v1/ratings/{rateeId}", new { Score = 5, Comment = "Great GM!", Role = "DungeonMaster" });

        Assert.Equal(HttpStatusCode.Created, rate.StatusCode);

        var ratings = await rater.GetAsync($"/api/v1/ratings/{rateeId}");
        Assert.Equal(HttpStatusCode.OK, ratings.StatusCode);
    }

    [Fact]
    public async Task RateSelf_ReturnsUnprocessableEntity()
    {
        var client = factory.CreateClient();
        var userId = await client.AuthenticateWithIdAsync();

        var rate = await client.PostAsJsonAsync($"/api/v1/ratings/{userId}", new { Score = 5, Comment = (string?)null, Role = "Player" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, rate.StatusCode);
    }

    [Fact]
    public async Task RateUser_InvalidScore_ReturnsUnprocessableEntity()
    {
        var rater = await factory.CreateClient().AuthenticateAsync();
        var rateeId = await RegisterUserAsync(factory.CreateClient());

        var rate = await rater.PostAsJsonAsync($"/api/v1/ratings/{rateeId}", new { Score = 6, Comment = (string?)null, Role = "Player" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, rate.StatusCode);
    }

    [Fact]
    public async Task RateUser_NonExistentRatee_ReturnsNotFound()
    {
        var rater = await factory.CreateClient().AuthenticateAsync();

        var rate = await rater.PostAsJsonAsync($"/api/v1/ratings/{Guid.NewGuid()}", new { Score = 5, Comment = (string?)null, Role = "Player" });

        Assert.Equal(HttpStatusCode.NotFound, rate.StatusCode);
    }

    [Fact]
    public async Task GetRatings_ForUnratedUser_ReturnsOkWithEmptyList()
    {
        var client = factory.CreateClient();
        var userId = await RegisterUserAsync(factory.CreateClient());

        var response = await client.GetAsync($"/api/v1/ratings/{userId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed record RegisterResponseDto(Guid UserId, string Username, string Email);
}
