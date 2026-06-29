using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Reference.Dnd5e;

public partial class Spells
{
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private SpellPagedResult? _result;
    private bool _loading = true;

    private string _search = "";
    private string _level = "";
    private string _school = "";
    private string _class = "";
    private int _page = 1;
    private const int PageSize = 40;

    private static readonly string[] _schools =
        ["Abjuration", "Conjuration", "Divination", "Enchantment",
         "Evocation", "Illusion", "Necromancy", "Transmutation"];

    private static readonly string[] _classes =
        ["Bard", "Cleric", "Druid", "Paladin", "Ranger",
         "Sorcerer", "Warlock", "Wizard"];

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            int? level = int.TryParse(_level, out var l) ? l : null;
            _result = await Api.GetDnd5eSpellsAsync(
                search: string.IsNullOrWhiteSpace(_search) ? null : _search,
                school: string.IsNullOrWhiteSpace(_school) ? null : _school,
                level:  level,
                @class: string.IsNullOrWhiteSpace(_class) ? null : _class,
                page:   _page,
                pageSize: PageSize);
        }
        catch { _result = null; }
        finally { _loading = false; }
    }

    private async Task ApplyFilters() { _page = 1; await LoadAsync(); }
    private async Task ResetFilters() { _search = ""; _level = ""; _school = ""; _class = ""; _page = 1; await LoadAsync(); }
    private async Task PrevPage() { if (_page > 1) { _page--; await LoadAsync(); } }
    private async Task NextPage() { if (_result is not null && _page < _result.TotalPages) { _page++; await LoadAsync(); } }

    private async Task OnSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await ApplyFilters();
    }

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
