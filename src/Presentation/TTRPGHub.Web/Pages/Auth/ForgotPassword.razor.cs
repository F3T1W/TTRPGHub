using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Auth;

public partial class ForgotPassword
{
    [Inject] private IApiClient Api { get; set; } = default!;

    private string _email = "";
    private bool _loading;
    private bool _sent;
    private string _error = "";

    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(_email)) return;
        _loading = true;
        _error = "";
        try
        {
            await Api.ForgotPasswordAsync(new ForgotPasswordRequest(_email));
            _sent = true;
        }
        catch
        {
            _error = "Произошла ошибка. Попробуйте позже.";
        }
        finally
        {
            _loading = false;
        }
    }
}
