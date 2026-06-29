using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Reference.Dnd5e;

public partial class Monsters
{
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private MonsterPagedResult? _result;
    private bool _loading = true;

    private string _search = "";
    private string _type = "";
    private string _size = "";
    private string _cr = "";
    private int _page = 1;
    private const int PageSize = 40;

    private static readonly string[] _types =
        ["Beast", "Dragon", "Fiend", "Giant", "Humanoid", "Monstrosity",
         "Ooze", "Plant", "Undead", "Aberration", "Celestial",
         "Construct", "Elemental", "Fey"];

    private static readonly string[] _sizes =
        ["Tiny", "Small", "Medium", "Large", "Huge", "Gargantuan"];

    private static readonly string[] _crs =
        ["0", "1/8", "1/4", "1/2", "1", "2", "3", "4", "5",
         "6", "7", "8", "9", "10", "11", "12", "13", "14", "15",
         "16", "17", "18", "19", "20", "21", "22", "23", "24", "30"];

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _result = await Api.GetDnd5eMonstersAsync(
                search: string.IsNullOrWhiteSpace(_search) ? null : _search,
                type:   string.IsNullOrWhiteSpace(_type) ? null : _type,
                size:   string.IsNullOrWhiteSpace(_size) ? null : _size,
                cr:     string.IsNullOrWhiteSpace(_cr) ? null : _cr,
                page:   _page,
                pageSize: PageSize);
        }
        catch { _result = null; }
        finally { _loading = false; }
    }

    private async Task ApplyFilters() { _page = 1; await LoadAsync(); }
    private async Task ResetFilters() { _search = ""; _type = ""; _size = ""; _cr = ""; _page = 1; await LoadAsync(); }
    private async Task PrevPage() { if (_page > 1) { _page--; await LoadAsync(); } }
    private async Task NextPage() { if (_result is not null && _page < _result.TotalPages) { _page++; await LoadAsync(); } }

    private async Task OnSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await ApplyFilters();
    }

    private static string CrBadge(string cr)
    {
        if (!double.TryParse(cr.Replace("/", "."), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var val))
            return "bg-secondary";
        return val switch
        {
            <= 0.5  => "bg-success",
            <= 4    => "bg-info text-dark",
            <= 10   => "bg-warning text-dark",
            <= 16   => "bg-danger",
            _       => "bg-dark border border-danger text-danger"
        };
    }
}
