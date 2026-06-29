using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Encounters;

public partial class Create
{
    [Parameter] public Guid CampaignId { get; set; }
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private readonly FormModel _model = new();
    private readonly List<EntryModel> _entries = [];
    private bool _saving;
    private string? _error;

    private void AddEntry() => _entries.Add(new EntryModel());
    private void RemoveEntry(int idx) => _entries.RemoveAt(idx);

    private async Task HandleSubmit()
    {
        _saving = true;
        _error = null;
        try
        {
            var response = await Api.CreateEncounterAsync(new CreateEncounterRequest(
                CampaignId, _model.Title, _model.Description,
                _model.Difficulty, _model.Notes,
                _entries.Select(e => new EncounterEntryInput(e.Name, e.Count, e.Notes)).ToList()));
            Nav.NavigateTo($"/encounters/{response.EncounterId}");
        }
        catch (Exception ex) { _error = ex.Message; }
        finally { _saving = false; }
    }

    private static string DifficultyLabel(EncounterDifficulty d) => d switch
    {
        EncounterDifficulty.Trivial => "Тривиальное",
        EncounterDifficulty.Easy    => "Лёгкое",
        EncounterDifficulty.Medium  => "Среднее",
        EncounterDifficulty.Hard    => "Сложное",
        EncounterDifficulty.Deadly  => "Смертельное",
        _                           => d.ToString()
    };

    private sealed class FormModel
    {
        [Required(ErrorMessage = "Введите название")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public EncounterDifficulty Difficulty { get; set; } = EncounterDifficulty.Medium;

        [MaxLength(5000)]
        public string? Notes { get; set; }
    }

    private sealed class EntryModel
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; } = 1;
        public string? Notes { get; set; }
    }
}
