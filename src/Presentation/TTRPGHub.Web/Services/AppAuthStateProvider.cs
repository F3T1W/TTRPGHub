using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace TTRPGHub.Services;

public sealed class AppAuthStateProvider(TokenStorage tokens) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await tokens.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            return Anonymous;

        var claims = ParseClaims(token);
        var exp    = claims.FirstOrDefault(c => c.Type == "exp");
        if (exp is not null && long.TryParse(exp.Value, out var expSeconds))
        {
            var expiry = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
            if (expiry < DateTimeOffset.UtcNow)
            {
                await tokens.ClearAsync();
                return Anonymous;
            }
        }

        var identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task NotifyLoginAsync(string accessToken, string refreshToken, Guid userId)
    {
        await tokens.SaveAsync(accessToken, refreshToken, userId);
        var claims   = ParseClaims(accessToken);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user     = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task NotifyLogoutAsync()
    {
        await tokens.ClearAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private static IEnumerable<Claim> ParseClaims(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var padded  = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        var json    = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
        var dict    = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? [];

        return dict.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString() ?? string.Empty));
    }
}
