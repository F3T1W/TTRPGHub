using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Pages.Characters;

public partial class CreateWizard
{
    [Inject] private IApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private string _systemSlug = "dnd5e";

    private List<RuleEntrySummaryDto> _races = [];
    private List<RuleEntrySummaryDto> _classes = [];
    private bool _loadingLists = true;
    private bool _submitting;
    private string? _error;

    private string _name = string.Empty;
    private string _raceSlug = string.Empty;
    private string _classSlug = string.Empty;
    private int _level = 1;

    private int _str = 10, _dex = 10, _con = 10, _int = 10, _wis = 10, _cha = 10;

    private IEnumerable<(string Label, Func<int> Get, Action<int> Set)> AbilityFields =>
    [
        ("СИЛ", () => _str, v => _str = v),
        ("ЛОВ", () => _dex, v => _dex = v),
        ("ТЕЛ", () => _con, v => _con = v),
        ("ИНТ", () => _int, v => _int = v),
        ("МДР", () => _wis, v => _wis = v),
        ("ХАР", () => _cha, v => _cha = v),
    ];

    protected override async Task OnInitializedAsync() => await LoadListsAsync();

    private async Task OnSystemChangedAsync(ChangeEventArgs e)
    {
        _systemSlug = e.Value?.ToString() ?? "dnd5e";
        _raceSlug = string.Empty;
        _classSlug = string.Empty;
        await LoadListsAsync();
    }

    private async Task LoadListsAsync()
    {
        _loadingLists = true;
        _error = null;
        try
        {
            var races = await Api.GetRuleEntriesAsync(_systemSlug, "race", pageSize: 100);
            var classes = await Api.GetRuleEntriesAsync(_systemSlug, "class", pageSize: 100);
            _races = races.Items;
            _classes = classes.Items;
        }
        catch
        {
            _error = "Не удалось загрузить список рас и классов из справочника.";
        }
        finally
        {
            _loadingLists = false;
        }
    }

    private static int ParseInt(object? value) =>
        int.TryParse(value?.ToString(), out var v) ? Math.Clamp(v, 1, 20) : 10;

    private async Task SubmitAsync()
    {
        _submitting = true;
        _error = null;
        try
        {
            var result = await Api.CreateCharacterFromRulesAsync(new CreateCharacterFromRulesRequest(
                _name, _systemSlug, _raceSlug, _classSlug, _level,
                _str, _dex, _con, _int, _wis, _cha));
            Nav.NavigateTo($"/characters/{result.CharacterId}");
        }
        catch
        {
            _error = "Не удалось создать персонажа. Попробуй ещё раз.";
        }
        finally
        {
            _submitting = false;
        }
    }
}
