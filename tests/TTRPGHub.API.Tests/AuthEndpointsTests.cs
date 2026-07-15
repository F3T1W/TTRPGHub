using System.Net;
using System.Net.Http.Json;

namespace TTRPGHub.API.Tests;

[Collection("Api")]
public class AuthEndpointsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@test.local";

    [Fact]
    public async Task Register_NewUser_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Username = $"user{Guid.NewGuid():N}"[..12],
            Email = UniqueEmail(),
            Password = "Sup3rSecret!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var email = UniqueEmail();
        var payload = new
        {
            Username = $"user{Guid.NewGuid():N}"[..12],
            Email = email,
            Password = "Sup3rSecret!"
        };

        await _client.PostAsJsonAsync("/api/auth/register", payload);
        var second = await _client.PostAsJsonAsync("/api/auth/register", payload with { Username = $"user{Guid.NewGuid():N}"[..12] });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_ThenLogin_ReturnsAccessToken()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Username = $"user{Guid.NewGuid():N}"[..12],
            Email = email,
            Password = "Sup3rSecret!"
        });

        var login = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "Sup3rSecret!" });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.False(string.IsNullOrEmpty(body!.AccessToken));
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnprocessableEntity()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Username = $"user{Guid.NewGuid():N}"[..12],
            Email = email,
            Password = "Sup3rSecret!"
        });

        var login = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "WrongPassword!" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, login.StatusCode);
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, string Username, Guid UserId);
}

[Collection("Api")]
public class HealthAndPublicEndpointsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_ReturnsSuccessStatusCode()
    {
        var response = await _client.GetAsync("/health");

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task GetRuleSystems_AnonymousAccess_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/rules/systems");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetCharacters_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/characters/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
