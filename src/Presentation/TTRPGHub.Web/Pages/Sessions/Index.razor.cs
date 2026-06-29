using TTRPGHub.Services;

namespace TTRPGHub.Pages.Sessions;

public partial class Index
{
    private List<SessionSummaryDto> _sessions = [];
    private bool _loading = true;
    private string? _error;
    private string _tab = "upcoming";

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task SwitchTab(string tab)
    {
        _tab = tab;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true; _error = null;
        try
        {
            _sessions = _tab == "upcoming"
                ? await Api.GetUpcomingSessionsAsync()
                : await Api.GetMySessionsAsync();
        }
        catch { _error = "Не удалось загрузить сессии."; }
        finally { _loading = false; }
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
