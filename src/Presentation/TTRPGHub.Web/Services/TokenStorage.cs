using Microsoft.JSInterop;

namespace TTRPGHub.Services;

public sealed class TokenStorage(IJSRuntime js)
{
    private const string AccessKey  = "ta_access";
    private const string RefreshKey = "ta_refresh";
    private const string UserIdKey  = "ta_user_id";

    public async Task<string?> GetAccessTokenAsync() =>
        await js.InvokeAsync<string?>("localStorage.getItem", AccessKey);

    public async Task<Guid?> GetUserIdAsync()
    {
        var raw = await js.InvokeAsync<string?>("localStorage.getItem", UserIdKey);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public async Task SaveAsync(string accessToken, string refreshToken, Guid userId)
    {
        await js.InvokeVoidAsync("localStorage.setItem", AccessKey,  accessToken);
        await js.InvokeVoidAsync("localStorage.setItem", RefreshKey, refreshToken);
        await js.InvokeVoidAsync("localStorage.setItem", UserIdKey,  userId.ToString());
    }

    public async Task ClearAsync()
    {
        await js.InvokeVoidAsync("localStorage.removeItem", AccessKey);
        await js.InvokeVoidAsync("localStorage.removeItem", RefreshKey);
        await js.InvokeVoidAsync("localStorage.removeItem", UserIdKey);
    }
}
