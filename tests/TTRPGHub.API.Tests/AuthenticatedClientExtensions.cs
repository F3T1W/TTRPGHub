using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TTRPGHub.API.Tests;

internal static class AuthenticatedClientExtensions
{
    public static async Task<HttpClient> AuthenticateAsync(this HttpClient client)
    {
        await client.AuthenticateWithIdAsync();
        return client;
    }

    public static async Task<Guid> AuthenticateWithIdAsync(this HttpClient client)
    {
        var email = $"user-{Guid.NewGuid():N}@test.local";
        var username = $"user{Guid.NewGuid():N}"[..12];
        const string password = "Sup3rSecret!";

        await client.PostAsJsonAsync("/api/auth/register", new { Username = username, Email = email, Password = password });
        var login = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        var body = await login.Content.ReadFromJsonAsync<LoginResponseDto>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return body!.UserId;
    }

    private sealed record LoginResponseDto(string AccessToken, string RefreshToken, string Username, Guid UserId);
}
