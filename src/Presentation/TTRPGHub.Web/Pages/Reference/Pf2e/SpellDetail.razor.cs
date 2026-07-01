using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Reference.Pf2e;

public partial class SpellDetail
{
    [Parameter] public Guid Id { get; set; }
    [Inject] private IApiClient Api { get; set; } = default!;

    private Pf2eSpellDetailDto? _spell;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        try { _spell = await Api.GetPf2eSpellAsync(Id); }
        catch { _spell = null; }
        finally { _loading = false; }
    }

    private static string LevelLabel(int level) =>
        level == 0 ? "Заговор" : $"Заклинание {level}-го уровня";

    private static string LevelBadge(int level) => level switch
    {
        0 => "bg-secondary",
        1 or 2 or 3 => "bg-info text-dark",
        4 or 5 or 6 => "bg-primary",
        7 or 8 => "bg-warning text-dark",
        9 => "bg-danger",
        _ => "bg-dark border border-warning text-warning"
    };
}
