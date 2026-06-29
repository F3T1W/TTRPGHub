using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Events;

public partial class Detail
{
    [Parameter] public Guid Id { get; set; }
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private TokenStorage Tokens { get; set; } = default!;

    private GameEventDetailDto? _event;
    private bool _loading = true;
    private bool _isOrganizer;
    private bool _isParticipant;
    private bool _acting;
    private string? _statusMessage;
    private bool _statusIsError;

    protected override async Task OnParametersSetAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _event = await Api.GetEventDetailAsync(Id);
            var myId = await Tokens.GetUserIdAsync();
            _isOrganizer = myId.HasValue && myId.Value == _event.OrganizerId;
            _isParticipant = myId.HasValue && _event.Participants.Any(p => p.UserId == myId.Value);
        }
        catch { _event = null; }
        finally { _loading = false; }
    }

    private async Task RegisterAsync()
    {
        _acting = true;
        _statusMessage = null;
        try
        {
            await Api.RegisterForEventAsync(Id);
            await LoadAsync();
            _statusMessage = "Вы успешно записались!";
        }
        catch (Exception ex) { _statusMessage = ex.Message; _statusIsError = true; }
        finally { _acting = false; }
    }

    private async Task UnregisterAsync()
    {
        _acting = true;
        _statusMessage = null;
        try
        {
            await Api.UnregisterFromEventAsync(Id);
            await LoadAsync();
            _statusMessage = "Регистрация отменена.";
            _statusIsError = false;
        }
        catch (Exception ex) { _statusMessage = ex.Message; _statusIsError = true; }
        finally { _acting = false; }
    }

    private async Task CancelAsync()
    {
        _acting = true;
        try
        {
            await Api.CancelEventAsync(Id);
            await LoadAsync();
        }
        catch { }
        finally { _acting = false; }
    }

    private static string FormatLabel(string f) => f switch
    {
        "Online" => "Онлайн",
        "Offline" => "Оффлайн",
        _ => "Гибрид"
    };

    private static string FormatColor(string f) => f switch
    {
        "Online" => "var(--ta-accent)",
        "Offline" => "#0d6efd",
        _ => "#198754"
    };
}
