using System.ComponentModel.DataAnnotations;

namespace TTRPGHub.Pages;

public partial class Register
{
    private readonly RegisterModel _model = new();
    private string? _error;
    private bool _loading;
    private bool _success;

    private async Task SubmitAsync()
    {
        _loading = true;
        _error   = null;

        try
        {
            await Api.RegisterAsync(new(_model.Username, _model.Email, _model.Password));
            _success = true;
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            _error = "Пользователь с таким email уже существует.";
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

    private sealed class RegisterModel
    {
        [Required(ErrorMessage = "Введите имя пользователя.")]
        [StringLength(32, MinimumLength = 3, ErrorMessage = "От 3 до 32 символов.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите email.")]
        [EmailAddress(ErrorMessage = "Некорректный email.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите пароль.")]
        [MinLength(8, ErrorMessage = "Минимум 8 символов.")]
        public string Password { get; set; } = string.Empty;
    }
}
