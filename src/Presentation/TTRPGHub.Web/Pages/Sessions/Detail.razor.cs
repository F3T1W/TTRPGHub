using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Sessions;

public partial class Detail
{
    [Parameter] public Guid Id { get; set; }
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private SessionDetailDto? _session;
    private bool _loading = true;
    private bool _actionLoading;
    private string? _error;
    private string? _actionError;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true; _error = null;
        try { _session = await Api.GetSessionDetailAsync(Id); }
        catch { _error = "Не удалось загрузить сессию."; }
        finally { _loading = false; }
    }

    private async Task JoinAsync()
    {
        _actionLoading = true; _actionError = null;
        try { await Api.JoinSessionAsync(Id); await LoadAsync(); }
        catch { _actionError = "Не удалось вступить в сессию."; }
        finally { _actionLoading = false; }
    }

    private async Task LeaveAsync()
    {
        _actionLoading = true; _actionError = null;
        try { await Api.LeaveSessionAsync(Id); await LoadAsync(); }
        catch { _actionError = "Не удалось покинуть сессию."; }
        finally { _actionLoading = false; }
    }

    private async Task ChangeStatusAsync(SessionStatus status)
    {
        _actionLoading = true; _actionError = null;
        try { await Api.ChangeSessionStatusAsync(Id, new ChangeStatusRequest(status)); await LoadAsync(); }
        catch { _actionError = "Не удалось изменить статус."; }
        finally { _actionLoading = false; }
    }

    internal async Task ExportAsync()
    {
        if (_session is null) return;
        var export = new ImportSessionRequest(
            _session.Title, _session.System, _session.ScheduledAt, _session.MaxPlayers, _session.Description);
        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
        await Js.InvokeVoidAsync("downloadJson", $"{_session.Title.Replace(" ", "_")}.json", json);
    }

    internal async Task DownloadIcsAsync()
    {
        if (_session is null) return;
        try
        {
            var ics = await Api.GetSessionIcsAsync(_session.Id);
            var bytes = System.Text.Encoding.UTF8.GetBytes(ics);
            var base64 = Convert.ToBase64String(bytes);
            var fileName = $"{_session.Title.Replace(" ", "_")}.ics";
            await Js.InvokeVoidAsync("downloadBase64File", base64, fileName, "text/calendar");
        }
        catch { /* ignore */ }
    }

    private static string StatusBadgeClass(SessionStatus s) => s switch
    {
        SessionStatus.Planned    => "bg-primary",
        SessionStatus.InProgress => "bg-success",
        SessionStatus.Completed  => "bg-secondary",
        SessionStatus.Cancelled  => "bg-danger",
        _ => "bg-secondary"
    };

    private static string StatusLabel(SessionStatus s) => s switch
    {
        SessionStatus.Planned    => "Запланирована",
        SessionStatus.InProgress => "Идёт игра",
        SessionStatus.Completed  => "Завершена",
        SessionStatus.Cancelled  => "Отменена",
        _ => s.ToString()
    };
}
