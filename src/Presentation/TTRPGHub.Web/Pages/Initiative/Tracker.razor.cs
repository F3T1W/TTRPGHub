using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Initiative;

public partial class Tracker : IAsyncDisposable
{
    [Parameter] public Guid Id { get; set; }
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private TokenStorage TokenStorage { get; set; } = default!;

    private TrackerDetailDto? _state;
    private readonly List<EditEntry> _editEntries = [];
    private HubConnection? _hub;
    private bool _loading = true;
    private bool _saving;
    private string _connectionStatus = "disconnected";
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        try { _state = await Api.GetTrackerDetailAsync(Id); }
        catch { _state = null; }
        finally { _loading = false; }

        if (_state is not null)
        {
            InitEditEntries();
            await ConnectHub();
        }
    }

    private void InitEditEntries()
    {
        _editEntries.Clear();
        if (_state is null) return;
        _editEntries.AddRange(_state.Entries.OrderBy(e => e.SortOrder).Select(e => new EditEntry
        {
            Name              = e.Name,
            Initiative        = e.Initiative,
            MaxHp             = e.MaxHp,
            CurrentHp         = e.CurrentHp,
            ArmorClass        = e.ArmorClass,
            IsPlayerCharacter = e.IsPlayerCharacter,
            Notes             = e.Notes,
        }));
    }

    private async Task ConnectHub()
    {
        var apiBase = Nav.BaseUri.TrimEnd('/').Replace(":5141", ":5014");
        _hub = new HubConnectionBuilder()
            .WithUrl($"{apiBase}/hubs/initiative", options =>
            {
                options.AccessTokenProvider = () => TokenStorage.GetAccessTokenAsync();
            })
            .WithAutomaticReconnect()
            .Build();

        _hub.On<TrackerDetailDto>("TrackerUpdated", dto =>
        {
            _state = dto;
            InvokeAsync(StateHasChanged);
        });

        _hub.Reconnecting += _ => { _connectionStatus = "reconnecting"; return InvokeAsync(StateHasChanged); };
        _hub.Reconnected  += _ => { _connectionStatus = "connected";    return InvokeAsync(StateHasChanged); };
        _hub.Closed       += _ => { _connectionStatus = "disconnected"; return InvokeAsync(StateHasChanged); };

        try
        {
            await _hub.StartAsync();
            await _hub.InvokeAsync("JoinTracker", Id.ToString());
            _connectionStatus = "connected";
        }
        catch { _connectionStatus = "disconnected"; }
    }

    private void AddEntryRow() => _editEntries.Add(new EditEntry());

    private async Task SaveEntries()
    {
        _saving = true;
        _error = null;
        try
        {
            await Api.SetTrackerEntriesAsync(Id, _editEntries.Select(e =>
                new TrackerEntryInput(e.Name, e.Initiative, e.MaxHp, e.CurrentHp,
                    e.ArmorClass, e.IsPlayerCharacter, e.Notes)).ToList());
        }
        catch (Exception ex) { _error = ex.Message; }
        finally { _saving = false; }
    }

    private async Task StartTracker()
    {
        try { await Api.StartTrackerAsync(Id); }
        catch (Exception ex) { _error = ex.Message; }
    }

    private async Task NextTurn()
    {
        try { await Api.NextTurnAsync(Id); }
        catch (Exception ex) { _error = ex.Message; }
    }

    private async Task PreviousTurn()
    {
        try { await Api.PreviousTurnAsync(Id); }
        catch (Exception ex) { _error = ex.Message; }
    }

    private async Task UpdateEntryHp(TrackerEntryDto entry, int hp)
    {
        try { await Api.UpdateTrackerEntryAsync(Id, entry.Id, new UpdateEntryRequest(hp, entry.Status, entry.Notes)); }
        catch (Exception ex) { _error = ex.Message; }
    }

    private async Task UpdateEntryStatus(TrackerEntryDto entry, EntryStatus status)
    {
        try { await Api.UpdateTrackerEntryAsync(Id, entry.Id, new UpdateEntryRequest(entry.CurrentHp, status, entry.Notes)); }
        catch (Exception ex) { _error = ex.Message; }
    }

    private static string HpColor(TrackerEntryDto e)
    {
        if (e.MaxHp <= 0) return "text-muted";
        var pct = (double)e.CurrentHp / e.MaxHp;
        return pct switch { >= 0.5 => "text-success", >= 0.25 => "text-warning", _ => "text-danger" };
    }

    private static string StatusBadge(EntryStatus s) => s switch
    {
        EntryStatus.Active      => "bg-success",
        EntryStatus.Unconscious => "bg-warning text-dark",
        EntryStatus.Dead        => "bg-danger",
        _                       => "bg-secondary"
    };

    private static string StatusLabel(EntryStatus s) => s switch
    {
        EntryStatus.Active      => "В бою",
        EntryStatus.Unconscious => "Без сознания",
        EntryStatus.Dead        => "Мёртв",
        _                       => s.ToString()
    };

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
        {
            if (_hub.State == HubConnectionState.Connected)
                await _hub.InvokeAsync("LeaveTracker", Id.ToString());
            await _hub.DisposeAsync();
        }
    }

    private sealed class EditEntry
    {
        public string Name { get; set; } = string.Empty;
        public int Initiative { get; set; }
        public int MaxHp { get; set; } = 10;
        public int CurrentHp { get; set; } = 10;
        public int ArmorClass { get; set; } = 10;
        public bool IsPlayerCharacter { get; set; }
        public string? Notes { get; set; }
    }
}
