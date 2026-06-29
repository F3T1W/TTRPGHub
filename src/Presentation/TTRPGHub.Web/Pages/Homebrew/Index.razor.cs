using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Homebrew;

public partial class Index
{
    [Inject] private IApiClient Api { get; set; } = default!;

    private HomebrewPagedResult? _data;
    private bool _loading = true;
    private int _page = 1;
    private string _query = "";
    private string _system = "";
    private string _typeFilter = "";

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _page = 1;
        HomebrewType? type = string.IsNullOrEmpty(_typeFilter) ? null : Enum.Parse<HomebrewType>(_typeFilter);
        _data = await Api.SearchHomebrewAsync(
            string.IsNullOrWhiteSpace(_query) ? null : _query,
            string.IsNullOrWhiteSpace(_system) ? null : _system,
            type, null, _page, 12);
        _loading = false;
    }

    private async Task GoToPageAsync(int page)
    {
        _loading = true;
        _page = page;
        HomebrewType? type = string.IsNullOrEmpty(_typeFilter) ? null : Enum.Parse<HomebrewType>(_typeFilter);
        _data = await Api.SearchHomebrewAsync(
            string.IsNullOrWhiteSpace(_query) ? null : _query,
            string.IsNullOrWhiteSpace(_system) ? null : _system,
            type, null, _page, 12);
        _loading = false;
    }

    private async Task OnSearchKey(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await LoadAsync();
    }

    private static string HomebrewTypeLabel(HomebrewType t) => t switch
    {
        HomebrewType.Spell => "Заклинание",
        HomebrewType.Monster => "Существо",
        HomebrewType.Class => "Класс",
        HomebrewType.Subclass => "Подкласс",
        HomebrewType.Race => "Раса",
        HomebrewType.Subrace => "Подраса",
        HomebrewType.Item => "Предмет",
        HomebrewType.Background => "Предыстория",
        HomebrewType.Feat => "Черта",
        HomebrewType.Other => "Другое",
        _ => t.ToString()
    };
}
