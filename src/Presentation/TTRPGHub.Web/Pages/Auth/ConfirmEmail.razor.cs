using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Auth;

public partial class ConfirmEmail
{
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private bool _loading = true;
    private bool _success;
    private string _error = "Токен недействителен или истёк.";

    protected override async Task OnInitializedAsync()
    {
        var token = GetQueryParam("token");
        if (string.IsNullOrEmpty(token))
        {
            _error = "Токен не указан.";
            _loading = false;
            return;
        }

        try
        {
            await Api.ConfirmEmailAsync(new ConfirmEmailRequest(token));
            _success = true;
        }
        catch
        {
            _success = false;
        }
        finally
        {
            _loading = false;
        }
    }

    private string? GetQueryParam(string name)
    {
        var query = new Uri(Nav.Uri).Query.TrimStart('?');
        foreach (var part in query.Split('&'))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0] == name)
                return Uri.UnescapeDataString(kv[1]);
        }
        return null;
    }
}
