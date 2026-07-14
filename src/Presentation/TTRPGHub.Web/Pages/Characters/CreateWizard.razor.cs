using Microsoft.AspNetCore.Components;
using System.Text.Json;
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

    // R.1 — свободный ("ANY") буст предка (сколько раз "ANY" встретилось в boost_codes расы) и
    // выбор ключевой характеристики, когда у класса их несколько на выбор (Боец/Следопыт STR/DEX).
    // Читаются из StatsJson выбранной расы/класса при их выборе (только для pf2e) — раньше эти
    // выборы молча пропускались автоматикой создания, теперь игрок делает их прямо в мастере.
    private static readonly (string Code, string Label)[] AbilityCodeLabels =
        [("STR", "Сила"), ("DEX", "Ловкость"), ("CON", "Телосложение"),
         ("INT", "Интеллект"), ("WIS", "Мудрость"), ("CHA", "Харизма")];

    private int _freeBoostSlots;
    private string[] _freeBoostChoices = [];
    private List<string> _keyAbilityOptions = [];
    private string? _keyAbilityChoice;

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
        ResetBoostChoices();
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

    private void ResetBoostChoices()
    {
        _freeBoostSlots = 0;
        _freeBoostChoices = [];
        _keyAbilityOptions = [];
        _keyAbilityChoice = null;
    }

    private async Task OnRaceChangedAsync(ChangeEventArgs e)
    {
        _raceSlug = e.Value?.ToString() ?? "";
        _freeBoostSlots = 0;
        _freeBoostChoices = [];
        if (_systemSlug != "pf2e" || string.IsNullOrEmpty(_raceSlug)) return;

        try
        {
            var race = await Api.GetRuleEntryDetailAsync("pf2e", "race", _raceSlug);
            var codes = ReadCodes(race.StatsJson, "boost_codes");
            _freeBoostSlots = codes.Count(c => c == "ANY");
            _freeBoostChoices = Enumerable.Repeat(AbilityCodeLabels[0].Code, _freeBoostSlots).ToArray();
        }
        catch { /* без подсказки — доведи вручную после создания, как раньше */ }
    }

    private async Task OnClassChangedAsync(ChangeEventArgs e)
    {
        _classSlug = e.Value?.ToString() ?? "";
        _keyAbilityOptions = [];
        _keyAbilityChoice = null;
        if (_systemSlug != "pf2e" || string.IsNullOrEmpty(_classSlug)) return;

        try
        {
            var characterClass = await Api.GetRuleEntryDetailAsync("pf2e", "class", _classSlug);
            var codes = ReadCodes(characterClass.StatsJson, "key_ability_codes");
            if (codes.Count > 1)
            {
                _keyAbilityOptions = codes;
                _keyAbilityChoice = codes[0];
            }
        }
        catch { /* без подсказки — доведи вручную после создания, как раньше */ }
    }

    private static List<string> ReadCodes(string json, string prop)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty(prop, out var el) || el.ValueKind != JsonValueKind.Array)
                return [];
            return el.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => s.Length > 0).ToList();
        }
        catch { return []; }
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
                _str, _dex, _con, _int, _wis, _cha,
                _freeBoostSlots > 0 ? [.. _freeBoostChoices] : null,
                _keyAbilityChoice));
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
