using Microsoft.AspNetCore.Components;
using TTRPGHub.Services;

namespace TTRPGHub.Components.Characters;

public partial class Pf2eStatsSheet
{
    [Parameter, EditorRequired] public CharacterDetailDto Character { get; set; } = default!;
    [Parameter] public EventCallback<CharacterDetailDto> CharacterChanged { get; set; }
    [Parameter] public bool AllowCreate { get; set; } = true;
    [Parameter] public bool ReadOnly { get; set; }
    [Parameter] public bool Compact { get; set; }

    [Inject] private IApiClient Api { get; set; } = default!;

    private string ColClass => Compact ? "col-12" : "col-lg-4";

    private bool _showPf2e;
    private bool _editingPf2e;
    private bool _savingPf2e;
    private string? _pf2eSaveError;
    private Pf2eLookups.Pf2eStatsModel _pf2e = new();

    private int _speedBonusFromFeats;
    private int _acBonusFromFeats;
    private int _perceptionBonusFromFeats;
    private Dictionary<string, int> _skillBonusFromFeats = [];
    private Dictionary<string, List<string>> _grantsByFeatSlug = [];
    private Dictionary<string, Pf2eLookups.Pf2eFeatChoiceSet> _choiceSetByFeatSlug = [];
    private Dictionary<string, List<Pf2eLookups.Pf2eFeatRollOption>> _rollOptionsByFeatSlug = [];

    // N.6 — расчётное КЗ по формуле (ComputeArmorClass) поверх экипированной брони; ABP здесь
    // не подключён — у листа персонажа нет сессии/стола, вариативные правила и без того нигде
    // не учитываются на этой странице (ровно как _proficiencyWithoutLevel никогда не передавался
    // в Pf2eBonus выше). Живой ABP-бонус к КЗ считается на столе (Table.razor.cs), где сессия есть.
    private Pf2eLookups.EquippedItemContext? _equippedArmorContext;
    private int _computedArmorClass;

    protected override async Task OnParametersSetAsync()
    {
        _pf2e = Pf2eLookups.Pf2eStatsModel.FromJson(Character.Pf2eStatsJson);
        _showPf2e = !string.IsNullOrWhiteSpace(Character.Pf2eStatsJson);
        await RefreshFeatModifiersAsync();
    }

    private void StartEditPf2e() { _editingPf2e = true; _pf2eSaveError = null; }

    private void CancelEditPf2e()
    {
        _pf2e = Pf2eLookups.Pf2eStatsModel.FromJson(Character.Pf2eStatsJson);
        _editingPf2e = false;
        _pf2eSaveError = null;
    }

    private async Task SavePf2eAsync()
    {
        _savingPf2e = true;
        _pf2eSaveError = null;
        try
        {
            var json = _pf2e.ToJson();
            await Api.UpdatePf2eStatsAsync(Character.Id, new UpdatePf2eStatsRequest(json));
            var updated = await Api.GetCharacterByIdAsync(Character.Id);
            _pf2e = Pf2eLookups.Pf2eStatsModel.FromJson(updated.Pf2eStatsJson);
            _editingPf2e = false;
            await CharacterChanged.InvokeAsync(updated);
        }
        catch { _pf2eSaveError = "Не удалось сохранить PF2e-лист."; }
        finally { _savingPf2e = false; }
    }

    private void AddInventoryItem() =>
        _pf2e = _pf2e with { Inventory = [.. _pf2e.Inventory, new Pf2eLookups.Pf2eInventoryItem("", 1, 0, false)] };

    private void RemoveInventoryItem(int index) =>
        _pf2e = _pf2e with { Inventory = [.. _pf2e.Inventory.Where((_, i) => i != index)] };

    // N.6 — тот же приём, что и у UpdateFeat: точное совпадение имени с каталогом снаряжения
    // привязывает Slug автоматически (нужен, чтобы формула КЗ нашла категорию/dex_cap/ac_bonus
    // экипированной брони — до этого Slug у предметов инвентаря никогда не проставлялся).
    private async Task UpdateInventoryItem(int index, Pf2eLookups.Pf2eInventoryItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.Name))
        {
            try
            {
                var page = await Api.GetRuleEntriesAsync("pf2e", "equipment", item.Name, 1, 5);
                var match = page.Items.FirstOrDefault(i => string.Equals(i.Title, item.Name, StringComparison.OrdinalIgnoreCase));
                item = item with { Slug = match?.Slug };
            }
            catch { item = item with { Slug = null }; }
        }
        else
        {
            item = item with { Slug = null };
        }

        var list = _pf2e.Inventory.ToList();
        list[index] = item;
        _pf2e = _pf2e with { Inventory = list };
        await RefreshArmorClassAsync();
    }

    private async Task RefreshArmorClassAsync()
    {
        _equippedArmorContext = null;
        var equippedSlugs = _pf2e.Inventory
            .Where(i => i.Equipped && i.Slug is not null)
            .Select(i => i.Slug!)
            .Distinct()
            .ToList();

        if (equippedSlugs.Count > 0)
        {
            try
            {
                var entries = await Api.GetRuleEntriesBySlugsAsync("pf2e", "equipment", new BatchSlugsRequest(equippedSlugs));
                _equippedArmorContext = entries
                    .Select(e => Pf2eLookups.ParseEquipmentContext(e.Slug, e.StatsJson))
                    .FirstOrDefault(c => c.ItemKind is "armor" or "shield");
            }
            catch { /* формула посчитает без брони */ }
        }

        _computedArmorClass = Pf2eLookups.ComputeArmorClass(
            AbilityModByKey("dex"), _pf2e.ArmorProficiencyRanks, Character.Level,
            proficiencyWithoutLevel: false, _equippedArmorContext, automaticBonusProgression: false)
            + _acBonusFromFeats;
    }

    private void AddAttack() =>
        _pf2e = _pf2e with { Attacks = [.. _pf2e.Attacks, new Pf2eLookups.Pf2eAttack("", 0, "str", "1d4", 0, null)] };

    private void RemoveAttack(int index) =>
        _pf2e = _pf2e with { Attacks = [.. _pf2e.Attacks.Where((_, i) => i != index)] };

    private void UpdateAttack(int index, Pf2eLookups.Pf2eAttack attack)
    {
        var list = _pf2e.Attacks.ToList();
        list[index] = attack;
        _pf2e = _pf2e with { Attacks = list };
    }

    private void AdjustHeroPoints(int delta) =>
        _pf2e = _pf2e with { HeroPoints = Math.Max(0, _pf2e.HeroPoints + delta) };

    private void AddResource() =>
        _pf2e = _pf2e with { Resources = [.. _pf2e.Resources, new Pf2eLookups.Pf2eResource("", 1, 1)] };

    private void RemoveResource(int index) =>
        _pf2e = _pf2e with { Resources = [.. _pf2e.Resources.Where((_, i) => i != index)] };

    private void UpdateResource(int index, Pf2eLookups.Pf2eResource resource)
    {
        var list = _pf2e.Resources.ToList();
        list[index] = resource;
        _pf2e = _pf2e with { Resources = list };
    }

    private void AddCustomRollOption() =>
        _pf2e = _pf2e with { CustomRollOptions = [.. _pf2e.CustomRollOptions, ""] };

    private void RemoveCustomRollOption(int index) =>
        _pf2e = _pf2e with { CustomRollOptions = [.. _pf2e.CustomRollOptions.Where((_, i) => i != index)] };

    private void UpdateCustomRollOption(int index, string value)
    {
        var list = _pf2e.CustomRollOptions.ToList();
        list[index] = value;
        _pf2e = _pf2e with { CustomRollOptions = list };
    }

    private void AddFeat() =>
        _pf2e = _pf2e with { Feats = [.. _pf2e.Feats, new Pf2eLookups.Pf2eFeat("", Character.Level)] };

    private void RemoveFeat(int index)
    {
        _pf2e = _pf2e with { Feats = [.. _pf2e.Feats.Where((_, i) => i != index)] };
        _ = RefreshFeatModifiersAsync();
    }

    private async Task UpdateFeat(int index, Pf2eLookups.Pf2eFeat feat)
    {
        if (!string.IsNullOrWhiteSpace(feat.Name))
        {
            try
            {
                var page = await Api.GetRuleEntriesAsync("pf2e", "feat", feat.Name, 1, 5);
                var match = page.Items.FirstOrDefault(i => string.Equals(i.Title, feat.Name, StringComparison.OrdinalIgnoreCase));
                feat = feat with { Slug = match?.Slug };
            }
            catch { feat = feat with { Slug = null }; }
        }
        else
        {
            feat = feat with { Slug = null };
        }

        var list = _pf2e.Feats.ToList();
        list[index] = feat;
        _pf2e = _pf2e with { Feats = list };
        await RefreshFeatModifiersAsync();
    }

    private async Task RefreshFeatModifiersAsync()
    {
        _speedBonusFromFeats = 0;
        _acBonusFromFeats = 0;
        _perceptionBonusFromFeats = 0;
        _skillBonusFromFeats = [];
        _grantsByFeatSlug = [];
        _choiceSetByFeatSlug = [];
        _rollOptionsByFeatSlug = [];

        var slugs = _pf2e.Feats.Where(f => f.Slug is not null).Select(f => f.Slug!).Distinct().ToList();
        if (slugs.Count == 0)
        {
            await RefreshArmorClassAsync();
            return;
        }

        try
        {
            var entries = await Api.GetRuleEntriesBySlugsAsync("pf2e", "feat", new BatchSlugsRequest(slugs));
            var statsJsonBySlug = entries.ToDictionary(e => e.Slug, e => e.StatsJson ?? "");
            var rollOptions = slugs.Select(s => $"feat:{s}").ToHashSet();
            Pf2eLookups.AddFeatChoiceRollOptions(rollOptions, _pf2e.Feats, statsJsonBySlug);
            foreach (var option in _pf2e.CustomRollOptions) rollOptions.Add(option);
            var facts = new Dictionary<string, double>
            {
                ["self:level"] = Character.Level,
                ["hp-percent"] = Character.MaxHitPoints > 0
                    ? 100.0 * Character.CurrentHitPoints / Character.MaxHitPoints
                    : 0,
            };
            foreach (var (skill, rank) in _pf2e.SkillRanks)
                facts[$"skill:{skill}:rank"] = rank switch { >= 8 => 4, >= 6 => 3, >= 4 => 2, >= 2 => 1, _ => 0 };

            foreach (var entry in entries)
            {
                var grants = Pf2eLookups.ParseFeatGrants(entry.StatsJson);
                if (grants.Count > 0)
                    _grantsByFeatSlug[entry.Slug] = grants;

                var choiceSet = Pf2eLookups.ParseFeatChoiceSet(entry.StatsJson);
                if (choiceSet is not null)
                    _choiceSetByFeatSlug[entry.Slug] = choiceSet;

                var featRollOptions = Pf2eLookups.ParseFeatRollOptions(entry.StatsJson);
                if (featRollOptions.Count > 0)
                    _rollOptionsByFeatSlug[entry.Slug] = featRollOptions;

                foreach (var mod in Pf2eLookups.ParseFeatModifiers(entry.StatsJson))
                {
                    if (!Pf2eLookups.PredicateEvaluator.Evaluate(mod.Predicate, rollOptions, facts))
                        continue;

                    if (mod.Selector == "land-speed") _speedBonusFromFeats += mod.Value;
                    else if (mod.Selector == "ac") _acBonusFromFeats += mod.Value;
                    else if (mod.Selector == "perception") _perceptionBonusFromFeats += mod.Value;
                    else _skillBonusFromFeats[mod.Selector] = _skillBonusFromFeats.GetValueOrDefault(mod.Selector) + mod.Value;
                }
            }
        }
        catch { /* ignore */ }

        await RefreshArmorClassAsync();
    }

    // N.10 — выбор ChoiceSet/тумблер RollOption вне режима редактирования листа (это игровое
    // действие за столом, а не правка сути персонажа) — поэтому сохраняется немедленно, без
    // ожидания явного "Сохранить" из общего флоу _editingPf2e/SavePf2eAsync.
    private async Task SetFeatChoiceAsync(int index, string? choice)
    {
        var feat = _pf2e.Feats[index];
        var list = _pf2e.Feats.ToList();
        list[index] = feat with { SelectedChoice = string.IsNullOrWhiteSpace(choice) ? null : choice };
        _pf2e = _pf2e with { Feats = list };
        await RefreshFeatModifiersAsync();
        await PersistFeatStateAsync();
    }

    private async Task ToggleFeatRollOptionAsync(int index, string option, bool enabled)
    {
        var feat = _pf2e.Feats[index];
        var toggled = (feat.ToggledOptions ?? []).ToList();
        if (enabled) { if (!toggled.Contains(option)) toggled.Add(option); }
        else toggled.Remove(option);
        var list = _pf2e.Feats.ToList();
        list[index] = feat with { ToggledOptions = toggled.Count > 0 ? toggled : null };
        _pf2e = _pf2e with { Feats = list };
        await RefreshFeatModifiersAsync();
        await PersistFeatStateAsync();
    }

    private async Task PersistFeatStateAsync()
    {
        try
        {
            await Api.UpdatePf2eStatsAsync(Character.Id, new UpdatePf2eStatsRequest(_pf2e.ToJson()));
            var updated = await Api.GetCharacterByIdAsync(Character.Id);
            await CharacterChanged.InvokeAsync(updated);
        }
        catch { /* выбор останется применённым в текущей сессии, повторим при следующем изменении */ }
    }

    private void AddKnownSpell() =>
        _pf2e = _pf2e with { KnownSpells = [.. _pf2e.KnownSpells, new Pf2eLookups.Pf2eKnownSpell("", 1, true)] };

    private void RemoveKnownSpell(int index) =>
        _pf2e = _pf2e with { KnownSpells = [.. _pf2e.KnownSpells.Where((_, i) => i != index)] };

    private void UpdateKnownSpell(int index, Pf2eLookups.Pf2eKnownSpell spell)
    {
        var list = _pf2e.KnownSpells.ToList();
        list[index] = spell;
        _pf2e = _pf2e with { KnownSpells = list };
    }

    private void AddKnownFormula() =>
        _pf2e = _pf2e with { KnownFormulas = [.. _pf2e.KnownFormulas, new Pf2eLookups.Pf2eKnownFormula("", 1)] };

    private void RemoveKnownFormula(int index) =>
        _pf2e = _pf2e with { KnownFormulas = [.. _pf2e.KnownFormulas.Where((_, i) => i != index)] };

    private async Task UpdateKnownFormula(int index, Pf2eLookups.Pf2eKnownFormula formula)
    {
        if (!string.IsNullOrWhiteSpace(formula.Name))
        {
            try
            {
                var page = await Api.GetRuleEntriesAsync("pf2e", "equipment", formula.Name, 1, 5);
                var match = page.Items.FirstOrDefault(i => string.Equals(i.Title, formula.Name, StringComparison.OrdinalIgnoreCase));
                formula = formula with { Slug = match?.Slug };
            }
            catch { formula = formula with { Slug = null }; }
        }
        else
        {
            formula = formula with { Slug = null };
        }

        var list = _pf2e.KnownFormulas.ToList();
        list[index] = formula;
        _pf2e = _pf2e with { KnownFormulas = list };
    }

    private string? _craftResultMessage;
    private readonly Random _craftRandom = new();

    // Craft-активность (N.2): бросок Ремесла против стандартного DC уровня формулы,
    // успех/крит.успех добавляет готовый предмет в инвентарь (Pf2eInventoryItem, Quantity 1).
    // Крафт — не часть режима "редактирования листа": результат сохраняется сразу, независимо
    // от _editingPf2e, чтобы игрок мог скрафтить предмет во время даунтайма без входа в правку.
    private async Task CraftFormula(Pf2eLookups.Pf2eKnownFormula formula)
    {
        var rank = _pf2e.SkillRanks.GetValueOrDefault("crafting");
        var bonus = Pf2eBonus(rank) + AbilityModByKey("int");
        var natural = _craftRandom.Next(1, 21);
        var total = natural + bonus;
        var dc = Pf2eLookups.StandardDc(formula.Level);
        var degree = Pf2eLookups.RollDegree(natural, total, dc);

        _craftResultMessage = $"«{formula.Name}»: d20({natural}) + {bonus} = {total} против DC {dc} — " + degree switch
        {
            Pf2eLookups.DegreeOfSuccess.CriticalSuccess => "критический успех, предмет добавлен в инвентарь.",
            Pf2eLookups.DegreeOfSuccess.Success => "успех, предмет добавлен в инвентарь.",
            Pf2eLookups.DegreeOfSuccess.Failure => "провал, материалы потрачены впустую.",
            _ => "критический провал, материалы потрачены впустую.",
        };

        if (degree is Pf2eLookups.DegreeOfSuccess.Success or Pf2eLookups.DegreeOfSuccess.CriticalSuccess)
        {
            _pf2e = _pf2e with
            {
                Inventory = [.. _pf2e.Inventory, new Pf2eLookups.Pf2eInventoryItem(formula.Name, 1, 0, false, formula.Slug)]
            };
            await SavePf2eAsync();
        }
    }

    private static readonly int[] SpellSlotLevels = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    private Pf2eLookups.Pf2eSpellSlotLevel SpellSlotFor(int level) =>
        _pf2e.SpellSlots.GetValueOrDefault(level, new Pf2eLookups.Pf2eSpellSlotLevel(0, 0));

    private void UpdateSpellSlot(int level, Pf2eLookups.Pf2eSpellSlotLevel slot)
    {
        var dict = new Dictionary<int, Pf2eLookups.Pf2eSpellSlotLevel>(_pf2e.SpellSlots) { [level] = slot };
        _pf2e = _pf2e with { SpellSlots = dict };
    }

    private int Pf2eBonus(int rank) => Pf2eLookups.Bonus(rank, Character.Level);

    private int AbilityModByKey(string key) => key switch
    {
        "str" => Character.StrengthModifier,
        "dex" => Character.DexterityModifier,
        "con" => Character.ConstitutionModifier,
        "int" => Character.IntelligenceModifier,
        "wis" => Character.WisdomModifier,
        "cha" => Character.CharismaModifier,
        _ => 0
    };

    private static readonly (string Key, string Label)[] Pf2eAbilities = Pf2eLookups.Abilities;
    private static readonly (string Key, string Label)[] Pf2eSaves = Pf2eLookups.Saves;
    private static readonly (string Key, string Label)[] Pf2eSkills = Pf2eLookups.Skills;
    private static readonly (int Value, string Label)[] Pf2eRanks = Pf2eLookups.Ranks;

    private static readonly (string Key, string Label)[] ArmorCategories =
        [("unarmored", "Без брони"), ("light", "Лёгкая"), ("medium", "Средняя"), ("heavy", "Тяжёлая")];

    private async Task UpdateArmorProficiencyRank(string category, int rank)
    {
        var ranks = new Dictionary<string, int>(_pf2e.ArmorProficiencyRanks) { [category] = rank };
        _pf2e = _pf2e with { ArmorProficiencyRanks = ranks };
        await RefreshArmorClassAsync();
    }
}
