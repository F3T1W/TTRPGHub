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

    private void UpdateInventoryItem(int index, Pf2eLookups.Pf2eInventoryItem item)
    {
        var list = _pf2e.Inventory.ToList();
        list[index] = item;
        _pf2e = _pf2e with { Inventory = list };
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

        var slugs = _pf2e.Feats.Where(f => f.Slug is not null).Select(f => f.Slug!).Distinct().ToList();
        if (slugs.Count == 0)
            return;

        try
        {
            var entries = await Api.GetRuleEntriesBySlugsAsync("pf2e", "feat", new BatchSlugsRequest(slugs));
            var rollOptions = slugs.Select(s => $"feat:{s}").ToHashSet();
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
}
