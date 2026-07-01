using TTRPGHub.Services;

namespace TTRPGHub.Pages.Party;

public partial class Index
{
    private List<SessionSummaryDto> _all = [];
    private List<SessionSummaryDto> _filtered = [];
    private List<string> _availableSystems = [];

    private bool _loading = true;
    private bool _loadingMore;
    private bool _hasMore;
    private string? _error;

    private string _search = "";
    private string _systemFilter = "";
    private int _spotsFilter;
    private string _formatFilter = "";
    private string _locationFilter = "";
    private int _page = 1;
    private const int PageSize = 20;

    private Guid? _joiningId;
    private Guid? _joinError;
    private string? _joinErrorText;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true; _error = null; _page = 1;
        try
        {
            var sessions = await Api.GetUpcomingSessionsAsync(page: 1, pageSize: PageSize);
            _all = sessions;
            _hasMore = sessions.Count == PageSize;
            _availableSystems = [.. sessions.Select(s => s.System).Distinct().OrderBy(x => x)];
            ApplyFilters();
        }
        catch { _error = "Не удалось загрузить сессии."; }
        finally { _loading = false; }
    }

    private async Task LoadMoreAsync()
    {
        _loadingMore = true;
        try
        {
            _page++;
            var more = await Api.GetUpcomingSessionsAsync(page: _page, pageSize: PageSize);
            _all.AddRange(more);
            _hasMore = more.Count == PageSize;
            var newSystems = more.Select(s => s.System).Distinct()
                .Where(s => !_availableSystems.Contains(s)).ToList();
            _availableSystems.AddRange(newSystems);
            _availableSystems.Sort();
            ApplyFilters();
        }
        catch
        {
            // ignored
        }
        finally { _loadingMore = false; }
    }

    private void ApplyFilters()
    {
        _filtered = _all
            .Where(s => string.IsNullOrWhiteSpace(_search) ||
                        s.Title.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
                        (s.Description?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false))
            .Where(s => string.IsNullOrEmpty(_systemFilter) || s.System == _systemFilter)
            .Where(s => _spotsFilter == 0 || (s.MaxPlayers - s.CurrentPlayers) >= _spotsFilter)
            .Where(s => string.IsNullOrEmpty(_formatFilter) || s.Format.ToString() == _formatFilter)
            .Where(s => string.IsNullOrWhiteSpace(_locationFilter) ||
                        (s.Location?.Contains(_locationFilter, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();
    }

    private void ResetFilters()
    {
        _search = ""; _systemFilter = ""; _spotsFilter = 0; _formatFilter = ""; _locationFilter = "";
        ApplyFilters();
    }

    private async Task JoinAsync(Guid sessionId)
    {
        _joiningId = sessionId; _joinError = null;
        try
        {
            await Api.JoinSessionAsync(sessionId);
            Nav.NavigateTo($"/sessions/{sessionId}");
        }
        catch (Refit.ApiException ex)
        {
            _joinError = sessionId;
            _joinErrorText = ex.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity
                ? "Уже в сессии или нет мест."
                : "Ошибка при вступлении.";
        }
        catch
        {
            _joinError = sessionId;
            _joinErrorText = "Ошибка при вступлении.";
        }
        finally { _joiningId = null; }
    }
}
