using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Reference.Pf2e;

public partial class Monsters : IDisposable
{
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private Pf2eLocaleService Locale { get; set; } = default!;
    [Inject] private ContentLanguageService Lang { get; set; } = default!;

    private Pf2eMonsterPagedResult? _result;
    private Dictionary<Guid, Pf2eLocalizedMonsterRow> _localized = [];
    private bool _loading = true;

    private string _search = "";
    private string _trait = "";
    private string _size = "";
    private string _level = "";
    private int _page = 1;
    private const int PageSize = 40;

    private static readonly string[] _traits =
        ["humanoid", "animal", "undead", "dragon", "giant", "construct",
         "plant", "fiend", "celestial", "monitor", "spirit"];

    private static readonly string[] _sizes =
        ["Крошечное", "Маленькое", "Среднее", "Большое", "Огромное", "Громадное"];

    private static readonly string[] _levels =
        ["-1", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10"];

    protected override async Task OnInitializedAsync()
    {
        await Lang.InitializeAsync();
        Lang.OnChanged += OnLanguageChanged;
        await LoadAsync();
    }

    private async void OnLanguageChanged() => await InvokeAsync(LoadAsync);

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            int? level = int.TryParse(_level, out var l) ? l : null;
            _result = await Api.GetPf2eMonstersAsync(
                search: string.IsNullOrWhiteSpace(_search) ? null : _search,
                trait:  string.IsNullOrWhiteSpace(_trait) ? null : _trait,
                size:   string.IsNullOrWhiteSpace(_size) ? null : _size,
                level:  level,
                page:   _page,
                pageSize: PageSize);

            _localized = new Dictionary<Guid, Pf2eLocalizedMonsterRow>();
            if (_result is not null)
            {
                foreach (var monster in _result.Items)
                    _localized[monster.Id] = await Locale.LocalizeAsync(monster);
            }
        }
        catch { _result = null; _localized = []; }
        finally { _loading = false; }
    }

    private Pf2eLocalizedMonsterRow Display(Pf2eMonsterSummaryDto monster) =>
        _localized.GetValueOrDefault(monster.Id, new Pf2eLocalizedMonsterRow(monster.Name, monster.Traits, monster.Size));

    private async Task ApplyFilters() { _page = 1; await LoadAsync(); }
    private async Task ResetFilters() { _search = ""; _trait = ""; _size = ""; _level = ""; _page = 1; await LoadAsync(); }
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

    public void Dispose() => Lang.OnChanged -= OnLanguageChanged;
}
