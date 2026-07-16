using Microsoft.JSInterop;
using NSubstitute;
using TTRPGHub.Services;

namespace TTRPGHub.Web.Tests;

public class TokenStorageTests
{
    [Fact]
    public async Task GetAccessTokenAsync_ReadsFromLocalStorage()
    {
        var js = Substitute.For<IJSRuntime>();
        js.InvokeAsync<string?>("localStorage.getItem", Arg.Is<object?[]>(a => (string)a[0]! == "ta_access"))
            .Returns(new ValueTask<string?>("abc123"));
        var storage = new TokenStorage(js);

        var token = await storage.GetAccessTokenAsync();

        Assert.Equal("abc123", token);
    }

    [Fact]
    public async Task GetUserIdAsync_ValidGuidStored_ReturnsParsedGuid()
    {
        var id = Guid.NewGuid();
        var js = Substitute.For<IJSRuntime>();
        js.InvokeAsync<string?>("localStorage.getItem", Arg.Any<object?[]>()).Returns(new ValueTask<string?>(id.ToString()));
        var storage = new TokenStorage(js);

        var result = await storage.GetUserIdAsync();

        Assert.Equal(id, result);
    }

    [Fact]
    public async Task GetUserIdAsync_NullOrInvalidValue_ReturnsNull()
    {
        var js = Substitute.For<IJSRuntime>();
        js.InvokeAsync<string?>("localStorage.getItem", Arg.Any<object?[]>()).Returns(new ValueTask<string?>((string?)null));
        var storage = new TokenStorage(js);

        Assert.Null(await storage.GetUserIdAsync());
    }

    [Fact]
    public async Task SaveAsync_WritesAllThreeKeys()
    {
        var js = Substitute.For<IJSRuntime>();
        var storage = new TokenStorage(js);
        var userId = Guid.NewGuid();

        await storage.SaveAsync("access-token", "refresh-token", userId);

        await js.Received(1).InvokeVoidAsync("localStorage.setItem", Arg.Is<object?[]>(a => (string)a[0]! == "ta_access" && (string)a[1]! == "access-token"));
        await js.Received(1).InvokeVoidAsync("localStorage.setItem", Arg.Is<object?[]>(a => (string)a[0]! == "ta_refresh" && (string)a[1]! == "refresh-token"));
        await js.Received(1).InvokeVoidAsync("localStorage.setItem", Arg.Is<object?[]>(a => (string)a[0]! == "ta_user_id" && (string)a[1]! == userId.ToString()));
    }

    [Fact]
    public async Task ClearAsync_RemovesAllThreeKeys()
    {
        var js = Substitute.For<IJSRuntime>();
        var storage = new TokenStorage(js);

        await storage.ClearAsync();

        await js.Received(1).InvokeVoidAsync("localStorage.removeItem", Arg.Is<object?[]>(a => (string)a[0]! == "ta_access"));
        await js.Received(1).InvokeVoidAsync("localStorage.removeItem", Arg.Is<object?[]>(a => (string)a[0]! == "ta_refresh"));
        await js.Received(1).InvokeVoidAsync("localStorage.removeItem", Arg.Is<object?[]>(a => (string)a[0]! == "ta_user_id"));
    }
}
