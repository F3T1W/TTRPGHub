using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Reference.Pf2e;

public partial class MonsterDetail
{
    [Parameter] public Guid Id { get; set; }
    [Inject] private IApiClient Api { get; set; } = default!;

    private Pf2eMonsterDetailDto? _monster;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        try { _monster = await Api.GetPf2eMonsterAsync(Id); }
        catch { _monster = null; }
        finally { _loading = false; }
    }

    private static string Modifier(int mod) => mod >= 0 ? $"+{mod}" : mod.ToString();

    private static string LevelBadge(int level) => level switch
    {
        <= 0 => "bg-success",
        <= 3 => "bg-info text-dark",
        <= 6 => "bg-warning text-dark",
        <= 10 => "bg-danger",
        _ => "bg-dark border border-danger text-danger"
    };
}
