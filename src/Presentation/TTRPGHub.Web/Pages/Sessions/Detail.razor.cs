using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Sessions;

public partial class Detail
{
    [Parameter] public Guid Id { get; set; }

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
