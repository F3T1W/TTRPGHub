using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using TTRPGHub.Services;

namespace TTRPGHub.Components.Characters;

public partial class PfsChronicleSheet
{
    [Parameter, EditorRequired] public CharacterDetailDto Character { get; set; } = default!;

    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private IConfiguration Config { get; set; } = default!;

    private List<ChronicleDto> _chronicles = [];
    private bool _loading = true;
    private bool _adding;
    private bool _saving;
    private string? _error;
    private string _apiBase = "http://localhost:5014";

    private string _scenarioName = "";
    private DateOnly _sessionDate = DateOnly.FromDateTime(DateTime.Today);
    private string _gmName = "";
    private string _faction = "";
    private int _goldEarned;
    private int _achievementPoints = 1;
    private string _boonsUsed = "";
    private string _notes = "";

    protected override async Task OnInitializedAsync()
    {
        _apiBase = Config["ApiBaseUrl"]?.TrimEnd('/') ?? _apiBase;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        try { _chronicles = await Api.GetChroniclesAsync(Character.Id); }
        catch { _error = "Не удалось загрузить хроники."; }
        finally { _loading = false; }
    }

    private int TotalGold => _chronicles.Sum(c => c.GoldEarned);
    private int TotalAp => _chronicles.Sum(c => c.AchievementPoints);

    private IEnumerable<(string Faction, int Count)> ReputationByFaction => _chronicles
        .Where(c => !string.IsNullOrWhiteSpace(c.Faction))
        .GroupBy(c => c.Faction!)
        .Select(g => (Faction: g.Key, Count: g.Count()));

    private void StartAdd() { _adding = true; _error = null; }

    private void CancelAdd()
    {
        _adding = false;
        _scenarioName = ""; _gmName = ""; _faction = ""; _boonsUsed = ""; _notes = "";
        _goldEarned = 0; _achievementPoints = 1;
        _sessionDate = DateOnly.FromDateTime(DateTime.Today);
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_scenarioName))
        {
            _error = "Укажите название сценария.";
            return;
        }

        _saving = true;
        _error = null;
        try
        {
            var request = new CreateChronicleRequest(
                _scenarioName, _sessionDate,
                string.IsNullOrWhiteSpace(_gmName) ? null : _gmName,
                string.IsNullOrWhiteSpace(_faction) ? null : _faction,
                _goldEarned, _achievementPoints,
                string.IsNullOrWhiteSpace(_boonsUsed) ? null : _boonsUsed,
                string.IsNullOrWhiteSpace(_notes) ? null : _notes);

            await Api.CreateChronicleAsync(Character.Id, request);
            await LoadAsync();
            CancelAdd();
        }
        catch { _error = "Не удалось сохранить chronicle sheet."; }
        finally { _saving = false; }
    }

    private async Task DeleteAsync(Guid chronicleId)
    {
        try
        {
            await Api.DeleteChronicleAsync(Character.Id, chronicleId);
            await LoadAsync();
        }
        catch { _error = "Не удалось удалить chronicle sheet."; }
    }

    private string PdfUrl(Guid chronicleId) => $"{_apiBase}/api/characters/{Character.Id}/chronicles/{chronicleId}/pdf";
}
