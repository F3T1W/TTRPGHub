using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Reference.Pf2e;

// N.1 — хазарды не проходят через Pf2eLocaleService (в отличие от монстров/заклинаний,
// M.4): у них нет предзагруженного английского набора, RU — единственный и основной язык
// контента, поэтому переключатель EN/RU здесь не нужен, показываем NameRu напрямую.
public partial class Hazards
{
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private Pf2eHazardPagedResult? _result;
    private bool _loading = true;

    private string _search = "";
    private string _level = "";
    private int _page = 1;
    private const int PageSize = 40;

    private static readonly string[] _levels =
        ["-1", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20"];

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            int? level = int.TryParse(_level, out var l) ? l : null;
            _result = await Api.GetPf2eHazardsAsync(
                search: string.IsNullOrWhiteSpace(_search) ? null : _search,
                level: level, page: _page, pageSize: PageSize);
        }
        catch { _result = null; }
        finally { _loading = false; }
    }

    private async Task ApplyFilters() { _page = 1; await LoadAsync(); }
    private async Task ResetFilters() { _search = ""; _level = ""; _page = 1; await LoadAsync(); }
    private async Task PrevPage() { if (_page > 1) { _page--; await LoadAsync(); } }
    private async Task NextPage() { if (_result is not null && _page < _result.TotalPages) { _page++; await LoadAsync(); } }

    private async Task OnSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await ApplyFilters();
    }

    private static string LevelBadge(int level) => level switch
    {
        <= 0 => "bg-success",
        <= 3 => "bg-info text-dark",
        <= 6 => "bg-warning text-dark",
        <= 10 => "bg-danger",
        _ => "bg-dark border border-danger text-danger"
    };
}
