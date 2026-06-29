using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Reference.Dnd5e;

public partial class SpellDetail
{
    [Parameter] public Guid Id { get; set; }
    [Inject] private IApiClient Api { get; set; } = default!;

    private SpellDetailDto? _spell;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        try { _spell = await Api.GetDnd5eSpellAsync(Id); }
        catch { _spell = null; }
        finally { _loading = false; }
    }

    private static string LevelSchoolLabel(int level, string school) =>
        level == 0 ? $"Заговор — {school}" : $"Заклинание {level}-го уровня — {school}";

    private static string LevelBadge(int level) => level switch
    {
        0 => "bg-secondary",
        1 or 2 => "bg-info text-dark",
        3 or 4 => "bg-primary",
        5 or 6 => "bg-warning text-dark",
        7 or 8 => "bg-danger",
        9 => "bg-dark border border-warning text-warning",
        _ => "bg-secondary"
    };
}
