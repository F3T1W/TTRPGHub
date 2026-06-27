using Microsoft.JSInterop;

namespace TTRPGHub.Web.Services;

public sealed class TokenStorage(IJSRuntime js)
{
    private const string AccessKey  = "ta_access";
    private const string RefreshKey = "ta_refresh";

    public async Task<string?> GetAccessTokenAsync() =>
        await js.InvokeAsync<string?>("localStorage.getItem", AccessKey);

    public async Task SaveAsync(string accessToken, string refreshToken)
    {
        await js.InvokeVoidAsync("localStorage.setItem", AccessKey,  accessToken);
        await js.InvokeVoidAsync("localStorage.setItem", RefreshKey, refreshToken);
    }

    public async Task ClearAsync()
    {
        await js.InvokeVoidAsync("localStorage.removeItem", AccessKey);
        await js.InvokeVoidAsync("localStorage.removeItem", RefreshKey);
    }
}
