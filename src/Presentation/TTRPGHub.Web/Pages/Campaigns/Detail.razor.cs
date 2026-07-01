using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Campaigns;

public partial class Detail
{
    [Parameter] public Guid Id { get; set; }
    [Inject] private IApiClient Api { get; set; } = default!;

    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private CampaignDetailDto? _campaign;
    private List<SessionNoteSummaryDto> _notes = [];
    private List<EncounterSummaryDto> _encounters = [];
    private List<TrackerSummaryDto> _trackers = [];
    private bool _loading = true;
    private bool _creatingTracker;
    private string? _error;

    protected override async Task OnInitializedAsync() => await Load();

    private async Task Load()
    {
        _loading = true;
        try
        {
            var campaignTask  = Api.GetCampaignDetailAsync(Id);
            var notesTask     = Api.GetNotesByCampaignAsync(Id);
            var encountersTask = Api.GetEncountersByCampaignAsync(Id);
            var trackersTask  = Api.GetTrackersByCampaignAsync(Id);
            await Task.WhenAll(campaignTask, notesTask, encountersTask, trackersTask);
            _campaign   = campaignTask.Result;
            _notes      = notesTask.Result;
            _encounters = encountersTask.Result;
            _trackers   = trackersTask.Result;
        }
        catch { _campaign = null; }
        finally { _loading = false; }
    }

    private async Task ChangeStatus(CampaignStatus status)
    {
        try { await Api.ChangeCampaignStatusAsync(Id, new ChangeCampaignStatusRequest(status)); await Load(); }
        catch (Exception ex) { _error = ex.Message; }
    }

    private async Task CreateTracker()
    {
        _creatingTracker = true;
        try
        {
            var resp = await Api.CreateTrackerAsync(new CreateTrackerRequest(Id, "Новый трекер"));
            Nav.NavigateTo($"/trackers/{resp.TrackerId}");
        }
        catch (Exception ex) { _error = ex.Message; _creatingTracker = false; }
    }

    private async Task RemoveParticipant(Guid userId)
    {
        try { await Api.RemoveCampaignParticipantAsync(Id, userId); await Load(); }
        catch (Exception ex) { _error = ex.Message; }
    }

    internal async Task ExportAsync()
    {
        if (_campaign is null) return;
        var export = new ImportCampaignRequest(_campaign.Title, _campaign.System, _campaign.Description);
        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
        await Js.InvokeVoidAsync("downloadJson", $"{_campaign.Title.Replace(" ", "_")}.json", json);
    }

    private static string StatusBadge(CampaignStatus status) => status switch
    {
        CampaignStatus.Active    => "bg-success",
        CampaignStatus.Paused    => "bg-warning text-dark",
        CampaignStatus.Completed => "bg-secondary",
        CampaignStatus.Archived  => "bg-dark border border-secondary",
        _                        => "bg-secondary"
    };

    private static string StatusLabel(CampaignStatus status) => status switch
    {
        CampaignStatus.Active    => "Активна",
        CampaignStatus.Paused    => "Пауза",
        CampaignStatus.Completed => "Завершена",
        CampaignStatus.Archived  => "Архив",
        _                        => status.ToString()
    };

    private static string RoleLabel(CampaignRole role) => role switch
    {
        CampaignRole.DungeonMaster => "Мастер",
        CampaignRole.Player        => "Игрок",
        _                          => role.ToString()
    };

    private static string DifficultyLabel(EncounterDifficulty d) => d switch
    {
        EncounterDifficulty.Trivial => "Тривиальное",
        EncounterDifficulty.Easy    => "Лёгкое",
        EncounterDifficulty.Medium  => "Среднее",
        EncounterDifficulty.Hard    => "Сложное",
        EncounterDifficulty.Deadly  => "Смертельное",
        _                           => d.ToString()
    };

    private static string DifficultyBadge(EncounterDifficulty d) => d switch
    {
        EncounterDifficulty.Trivial => "bg-success",
        EncounterDifficulty.Easy    => "bg-info text-dark",
        EncounterDifficulty.Medium  => "bg-warning text-dark",
        EncounterDifficulty.Hard    => "bg-orange text-dark",
        EncounterDifficulty.Deadly  => "bg-danger",
        _                           => "bg-secondary"
    };
}
