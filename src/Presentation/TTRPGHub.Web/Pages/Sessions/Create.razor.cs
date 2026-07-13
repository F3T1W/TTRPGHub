using System.ComponentModel.DataAnnotations;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Sessions;

public partial class Create
{
    private readonly SessionFormModel _form = new() { MaxPlayers = 5, ScheduledAt = DateTime.Now.AddDays(3) };
    private bool _saving;
    private string? _error;

    private static readonly string[] Systems =
    [
        "D&D 5e", "Pathfinder 2e", "Call of Cthulhu", "Savage Worlds",
        "FATE Core", "Shadowrun", "Warhammer Fantasy", "Cyberpunk RED", "Другая"
    ];

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var userId = await Tokens.GetUserIdAsync();
            if (userId is { } id)
            {
                var profile = await Api.GetUserProfileAsync(id);
                if (!string.IsNullOrWhiteSpace(profile.City))
                    _form.Location = profile.City;
            }
        }
        catch { /* необязательное автозаполнение — молча пропускаем */ }
    }

    private async Task SubmitAsync()
    {
        _saving = true; _error = null;
        try
        {
            var response = await Api.CreateSessionAsync(new CreateSessionRequest(
                _form.Title!, _form.Description, _form.System!,
                _form.MaxPlayers, _form.ScheduledAt, _form.Format, _form.Location));
            Nav.NavigateTo($"/sessions/{response.SessionId}");
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        {
            _error = "Проверьте заполненные поля.";
        }
        catch { _error = "Ошибка при создании сессии."; }
        finally { _saving = false; }
    }

    private sealed class SessionFormModel
    {
        [Required(ErrorMessage = "Обязательное поле")]
        [MaxLength(200)]
        public string? Title { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Выберите систему")]
        public string? System { get; set; }

        [Range(2, 10)]
        public int MaxPlayers { get; set; }

        public DateTime ScheduledAt { get; set; }

        public SessionFormat Format { get; set; } = SessionFormat.Online;

        [MaxLength(300)]
        public string? Location { get; set; }
    }
}
