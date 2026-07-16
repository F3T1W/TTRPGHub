using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;
using NSubstitute;
using TTRPGHub.Services;

namespace TTRPGHub.Web.Tests;

public class AppAuthStateProviderTests
{
    private static string MakeJwt(object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var payloadSegment = Base64UrlEncode(Encoding.UTF8.GetBytes(json));
        return $"header.{payloadSegment}.signature";
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static (AppAuthStateProvider Provider, IJSRuntime Js) CreateProvider(string? storedToken)
    {
        var js = Substitute.For<IJSRuntime>();
        js.InvokeAsync<string?>("localStorage.getItem", Arg.Is<object?[]>(a => (string)a[0]! == "ta_access"))
            .Returns(new ValueTask<string?>(storedToken));
        var provider = new AppAuthStateProvider(new TokenStorage(js));
        return (provider, js);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_NoStoredToken_ReturnsAnonymous()
    {
        var (provider, _) = CreateProvider(storedToken: null);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity!.IsAuthenticated);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ValidUnexpiredToken_ReturnsAuthenticatedUserWithClaims()
    {
        var exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var token = MakeJwt(new { sub = "user-123", unique_name = "grog", exp });
        var (provider, _) = CreateProvider(token);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.True(state.User.Identity!.IsAuthenticated);
        Assert.Equal("user-123", state.User.FindFirst("sub")!.Value);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ExpiredToken_ClearsStorageAndReturnsAnonymous()
    {
        var exp = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds();
        var token = MakeJwt(new { sub = "user-123", exp });
        var (provider, js) = CreateProvider(token);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity!.IsAuthenticated);
        await js.Received(1).InvokeVoidAsync("localStorage.removeItem", Arg.Is<object?[]>(a => (string)a[0]! == "ta_access"));
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_TokenWithoutExpClaim_IsTreatedAsValid()
    {
        var token = MakeJwt(new { sub = "user-123" });
        var (provider, _) = CreateProvider(token);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.True(state.User.Identity!.IsAuthenticated);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_PayloadNeedingBase64UrlChars_DecodesWithoutThrowing()
    {
        // JsonSerializer's default encoder escapes '+'/'/' as +//, so round-tripping a
        // payload through it can never produce those source bytes. Build the JSON by hand instead
        // — '+' and '/' don't need escaping inside a JSON string — so this specific filler text
        // (verified offline to hit) survives into the payload and, once base64url-encoded,
        // reliably contains '-'/'_' (base64url's replacement for '+'/'/') — the exact bytes that
        // broke the old Convert.FromBase64String call without a base64url -> base64 translation
        // step first.
        var exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        const string filler = "yQ>4+74>v>!`m8BE 3VfPpjI1zc";
        var json = $$"""{"sub":"{{filler}}","exp":{{exp}}}""";
        var payloadSegment = Base64UrlEncode(Encoding.UTF8.GetBytes(json));
        var token = $"header.{payloadSegment}.signature";
        Assert.True(payloadSegment.Any(c => c is '-' or '_'), "Test setup should produce a base64url payload containing '-' or '_'.");
        var (provider, _) = CreateProvider(token);

        var state = await provider.GetAuthenticationStateAsync();

        Assert.True(state.User.Identity!.IsAuthenticated);
    }

    [Fact]
    public async Task NotifyLoginAsync_SavesTokenAndRaisesAuthenticationStateChanged()
    {
        var js = Substitute.For<IJSRuntime>();
        var provider = new AppAuthStateProvider(new TokenStorage(js));
        var exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var token = MakeJwt(new { sub = "user-123", exp });
        AuthenticationStateChangedRaised? raised = null;
        provider.AuthenticationStateChanged += async task => raised = new AuthenticationStateChangedRaised((await task).User.Identity!.IsAuthenticated);

        await provider.NotifyLoginAsync(token, "refresh-token", Guid.NewGuid());

        await js.Received(1).InvokeVoidAsync("localStorage.setItem", Arg.Is<object?[]>(a => (string)a[0]! == "ta_access"));
        Assert.True(raised?.IsAuthenticated);
    }

    [Fact]
    public async Task NotifyLogoutAsync_ClearsStorageAndRaisesAnonymousState()
    {
        var js = Substitute.For<IJSRuntime>();
        var provider = new AppAuthStateProvider(new TokenStorage(js));
        AuthenticationStateChangedRaised? raised = null;
        provider.AuthenticationStateChanged += async task => raised = new AuthenticationStateChangedRaised((await task).User.Identity!.IsAuthenticated);

        await provider.NotifyLogoutAsync();

        await js.Received(1).InvokeVoidAsync("localStorage.removeItem", Arg.Is<object?[]>(a => (string)a[0]! == "ta_access"));
        Assert.False(raised?.IsAuthenticated);
    }

    private sealed record AuthenticationStateChangedRaised(bool IsAuthenticated);
}
