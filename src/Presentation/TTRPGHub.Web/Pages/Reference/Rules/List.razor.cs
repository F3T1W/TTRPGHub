using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Reference.Rules;

public partial class List : IDisposable
{
    [Parameter] public string SystemSlug { get; set; } = "";
    [Parameter] public string Category { get; set; } = "";
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private Pf2eLocaleService Locale { get; set; } = default!;
    [Inject] private ContentLanguageService Lang { get; set; } = default!;

    private RuleEntryPageDto? _result;
    private Dictionary<string, string> _entryTitles = new(StringComparer.OrdinalIgnoreCase);
    private bool _loading = true;
    private string? _error;
    private bool _canManage;
    private string? _systemName;

    private string _search = "";
    private int _page = 1;
    private const int PageSize = 30;

    // D&D5e держит заклинания/монстров на отдельных legacy-страницах (/spells, /monsters) —
    // здесь показываем только категории, которых там нет. Остальные системы (PF2e, кастомные)
    // не имеют legacy-страниц вообще, поэтому показываем полный список.
    private static readonly string[] Dnd5eCategories = ["class", "race", "feat", "condition"];
    private static readonly string[] FullCategories =
        ["spell", "monster", "class", "race", "feat", "action", "condition", "equipment", "background", "rule"];

    private string[] AllCategories => SystemSlug == "dnd5e" ? Dnd5eCategories : FullCategories;

    private bool _isOfficialSystem = true;

    private IEnumerable<string> OtherCategories =>
        AllCategories.Where(c => !string.Equals(c, Category, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnParametersSetAsync()
    {
        await Lang.InitializeAsync();
        Lang.OnChanged -= OnLanguageChanged;
        Lang.OnChanged += OnLanguageChanged;
        _page = 1;
        _search = "";
        await LoadSystemInfoAsync();
        await LoadAsync();
    }

    private async void OnLanguageChanged() => await InvokeAsync(async () => { await LoadAsync(); });

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
            await BuildEntryTitlesAsync();
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

    private async Task BuildEntryTitlesAsync()
    {
        _entryTitles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (SystemSlug != "pf2e" || _result is null) return;
        var cat = Category.ToLowerInvariant();
        foreach (var item in _result.Items)
            _entryTitles[item.Slug] = await Locale.NameAsync(cat, item.Slug, item.Title);
    }

    private string EntryTitle(RuleEntrySummaryDto item) =>
        _entryTitles.GetValueOrDefault(item.Slug, item.Title);

    public void Dispose() => Lang.OnChanged -= OnLanguageChanged;

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
        "action" => "Действия",
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
        "action" => "🎯",
        "condition" => "☣️",
        _ => "📖"
    };
}
