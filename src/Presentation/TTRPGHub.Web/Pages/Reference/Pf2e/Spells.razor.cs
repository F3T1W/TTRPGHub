using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Reference.Pf2e;

public partial class Spells
{
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private Pf2eSpellPagedResult? _result;
    private bool _loading = true;

    private string _search = "";
    private string _level = "";
    private string _tradition = "";
    private string _trait = "";
    private int _page = 1;
    private const int PageSize = 40;

    private static readonly string[] _traditions =
        ["arcane", "divine", "primal", "occult"];

    private static readonly string[] _traits =
        ["fire", "cold", "mental", "healing", "evocation", "abjuration",
         "illusion", "transmutation", "conjuration", "enchantment"];

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            int? level = int.TryParse(_level, out var l) ? l : null;
            _result = await Api.GetPf2eSpellsAsync(
                search: string.IsNullOrWhiteSpace(_search) ? null : _search,
                tradition: string.IsNullOrWhiteSpace(_tradition) ? null : _tradition,
                level:  level,
                trait:  string.IsNullOrWhiteSpace(_trait) ? null : _trait,
                page:   _page,
                pageSize: PageSize);
        }
        catch { _result = null; }
        finally { _loading = false; }
    }

    private async Task ApplyFilters() { _page = 1; await LoadAsync(); }
    private async Task ResetFilters() { _search = ""; _level = ""; _tradition = ""; _trait = ""; _page = 1; await LoadAsync(); }
    private async Task PrevPage() { if (_page > 1) { _page--; await LoadAsync(); } }
    private async Task NextPage() { if (_result is not null && _page < _result.TotalPages) { _page++; await LoadAsync(); } }

    private async Task OnSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await ApplyFilters();
    }

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
