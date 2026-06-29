using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Auth;

public partial class ResetPassword
{
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private string _token = "";
    private string _password = "";
    private string _confirm = "";
    private bool _loading;
    private bool _success;
    private bool _noToken;
    private string _error = "";

    protected override void OnInitialized()
    {
        var token = GetQueryParam("token");
        if (string.IsNullOrEmpty(token))
            _noToken = true;
        else
            _token = token;
    }

    private async Task SubmitAsync()
    {
        _error = "";
        if (_password.Length < 8) { _error = "Пароль должен быть не менее 8 символов."; return; }
        if (_password != _confirm) { _error = "Пароли не совпадают."; return; }

        _loading = true;
        try
        {
            await Api.ResetPasswordAsync(new ResetPasswordRequest(_token, _password));
            _success = true;
        }
        catch
        {
            _error = "Токен недействителен или истёк.";
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
