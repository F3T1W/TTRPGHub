using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Encounters;

public partial class Detail
{
    [Parameter] public Guid Id { get; set; }
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private EncounterDetailDto? _encounter;
    private bool _loading = true;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        try { _encounter = await Api.GetEncounterDetailAsync(Id); }
        catch { _encounter = null; }
        finally { _loading = false; }
    }

    private async Task Delete()
    {
        try
        {
            await Api.DeleteEncounterAsync(Id);
            Nav.NavigateTo($"/campaigns/{_encounter!.CampaignId}");
        }
        catch (Exception ex) { _error = ex.Message; }
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
