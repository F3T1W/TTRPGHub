using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Refit;

namespace TTRPGHub.Pages;

public partial class Login
{
    [SupplyParameterFromQuery] private string? ReturnUrl { get; set; }

    private readonly LoginModel _model = new();
    private string? _error;
    private bool _loading;

    private async Task SubmitAsync()
    {
        _loading = true;
        _error   = null;

        try
        {
            var resp = await Api.LoginAsync(new(_model.Email, _model.Password));
            await AuthProvider.NotifyLoginAsync(resp.AccessToken, resp.RefreshToken, resp.UserId);
            Nav.NavigateTo(ReturnUrl ?? "/");
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        {
            _error = "Неверный email или пароль.";
        }
        catch
        {
            _error = "Ошибка соединения с сервером.";
        }
        finally
        {
            _loading = false;
        }
    }

    private sealed class LoginModel
    {
        [Required(ErrorMessage = "Введите email.")]
        [EmailAddress(ErrorMessage = "Некорректный email.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите пароль.")]
        public string Password { get; set; } = string.Empty;
    }
}
