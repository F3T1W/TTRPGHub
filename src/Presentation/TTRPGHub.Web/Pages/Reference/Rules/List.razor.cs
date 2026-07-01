using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Reference.Rules;

public partial class List
{
    [Parameter] public string SystemSlug { get; set; } = "";
    [Parameter] public string Category { get; set; } = "";
    [Inject] private IApiClient Api { get; set; } = default!;

    private RuleEntryPageDto? _result;
    private bool _loading = true;
    private string? _error;
    private bool _canManage;
    private string? _systemName;

    private string _search = "";
    private int _page = 1;
    private const int PageSize = 30;

    private static readonly string[] OfficialCategories = ["class", "race", "feat", "condition"];
    private static readonly string[] CustomCategories =
        ["spell", "monster", "class", "race", "feat", "condition", "equipment", "background", "rule"];

    private string[] AllCategories => _isOfficialSystem ? OfficialCategories : CustomCategories;

    private bool _isOfficialSystem = true;

    private IEnumerable<string> OtherCategories =>
        AllCategories.Where(c => !string.Equals(c, Category, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnParametersSetAsync()
    {
        _page = 1;
        _search = "";
        await LoadSystemInfoAsync();
        await LoadAsync();
    }

    private async Task LoadSystemInfoAsync()
    {
        try
        {
            var systems = await Api.GetGameSystemsAsync();
            var system = systems.FirstOrDefault(s => s.Slug == SystemSlug);
            if (system is not null)
            {
                _systemName = system.Name;
                _isOfficialSystem = system.IsOfficial;
                _canManage = system.IsMine;
            }
        }
        catch { /* если не удалось — просто покажем slug вместо названия, без кнопки управления */ }
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _result = await Api.GetRuleEntriesAsync(
                SystemSlug, Category,
                search: string.IsNullOrWhiteSpace(_search) ? null : _search,
                page: _page, pageSize: PageSize);
        }
        catch
        {
            _result = null;
            _error = "Не удалось загрузить справочник.";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task ApplyFilters() { _page = 1; await LoadAsync(); }
    private async Task ResetFilters() { _search = ""; _page = 1; await LoadAsync(); }
    private async Task PrevPage() { if (_page > 1) { _page--; await LoadAsync(); } }
    private async Task NextPage() { if (_result is not null && _page < _result.TotalPages) { _page++; await LoadAsync(); } }

    private async Task OnSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await ApplyFilters();
    }

    private string SystemLabel(string slug) => slug switch
    {
        "dnd5e" => "D&D 5e",
        "pf2e" => "Pathfinder 2e",
        _ => _systemName ?? slug
    };

    private static string CategoryLabel(string category) => category.ToLowerInvariant() switch
    {
        "spell" => "Заклинания",
        "monster" => "Монстры",
        "class" => "Классы",
        "race" => "Расы",
        "feat" => "Фиты",
        "condition" => "Состояния",
        "equipment" => "Снаряжение",
        "background" => "Предыстории",
        "rule" => "Правила",
        _ => category
    };

    private static string CategoryIcon(string category) => category.ToLowerInvariant() switch
    {
        "spell" => "🔮",
        "monster" => "🐲",
        "class" => "⚔️",
        "race" => "🧬",
        "feat" => "⭐",
        "condition" => "☣️",
        _ => "📖"
    };
}
