using System.Text.Json;

namespace TTRPGHub.Services;

// L.5 — упрощённый импорт экспорта актёра Foundry VTT (система pf2e).
// Берём только то, что нужно для Character + Pf2eStatsJson: HP, AC, скорость, характеристики,
// владения, инвентарь, фиты, атаки, заклинания. Rule elements, ActiveEffects и вложенные
// контейнеры сознательно игнорируются — полный дамп Foundry нестабилен как публичный контракт.
public static class FoundryActorImporter
{
    private static readonly string[] AbilityKeys = ["str", "dex", "con", "int", "wis", "cha"];

    public static bool IsFoundryActorExport(JsonElement root) =>
        root.ValueKind == JsonValueKind.Object
        && root.TryGetProperty("type", out var type)
        && type.ValueKind == JsonValueKind.String
        && type.GetString() == "character"
        && root.TryGetProperty("system", out var system)
        && system.ValueKind == JsonValueKind.Object
        && (
            system.TryGetProperty("details", out _)
            || system.TryGetProperty("abilities", out _)
            || system.TryGetProperty("attributes", out _));

    public static (ImportCharacterRequest Request, Pf2eLookups.Pf2eStatsModel Stats) Parse(JsonElement root)
    {
        var system = root.GetProperty("system");
        var details = system.TryGetProperty("details", out var det) && det.ValueKind == JsonValueKind.Object ? det : default;
        var attributes = system.TryGetProperty("attributes", out var attr) && attr.ValueKind == JsonValueKind.Object ? attr : default;
        var abilities = system.TryGetProperty("abilities", out var ab) && ab.ValueKind == JsonValueKind.Object ? ab : default;
        var saves = system.TryGetProperty("saves", out var sv) && sv.ValueKind == JsonValueKind.Object ? sv : default;
        var skills = system.TryGetProperty("skills", out var sk) && sk.ValueKind == JsonValueKind.Object ? sk : default;
        var proficiencies = system.TryGetProperty("proficiencies", out var pr) && pr.ValueKind == JsonValueKind.Object ? pr : default;
        var resources = system.TryGetProperty("resources", out var res) && res.ValueKind == JsonValueKind.Object ? res : default;
        var perception = system.TryGetProperty("perception", out var perc) && perc.ValueKind == JsonValueKind.Object ? perc : default;

        var items = EnumerateItems(root).ToList();
        var ancestryItem = FindItem(items, "ancestry");
        var heritageItem = FindItem(items, "heritage");
        var classItem = FindItem(items, "class");
        var backgroundItem = FindItem(items, "background");

        var name = GetString(root, "name") ?? "Без имени";
        var level = Math.Clamp(GetNestedInt(details, "level", "value", 1), 1, 20);

        int AbilityScore(string key)
        {
            if (abilities.ValueKind != JsonValueKind.Object || !abilities.TryGetProperty(key, out var ability))
                return 10;
            var value = GetNestedInt(ability, "value");
            if (value > 0) return value;
            var baseScore = GetNestedInt(ability, "base");
            if (baseScore > 0) return baseScore;
            var mod = GetNestedInt(ability, "mod");
            return mod != 0 ? mod * 2 + 10 : 10;
        }

        var str = AbilityScore("str");
        var dex = AbilityScore("dex");
        var con = AbilityScore("con");
        var intScore = AbilityScore("int");
        var wis = AbilityScore("wis");
        var cha = AbilityScore("cha");

        var maxHp = Math.Max(1, GetNestedInt(attributes, "hp", "max", GetNestedInt(attributes, "hp", "value", 1)));
        var currentHp = Math.Clamp(GetNestedInt(attributes, "hp", "value", maxHp), 0, maxHp);
        var ac = GetNestedInt(attributes, "ac", "value", 10);
        var speed = ResolveSpeed(system, attributes);

        var classHpDie = classItem is { } cls && GetNestedInt(cls, "system", "hp") is var die and > 0 ? die : 8;

        var stats = new Pf2eLookups.Pf2eStatsModel
        {
            KeyAbility = GetNestedString(details, "keyability", "value") ?? "str",
            PerceptionRank = MapRank(GetNestedInt(perception, "rank")),
            ClassDcRank = ResolveClassDcRank(proficiencies),
            HeroPoints = GetNestedInt(resources, "heroPoints", "value", 1),
        };

        foreach (var saveKey in new[] { "fortitude", "reflex", "will" })
        {
            if (saves.ValueKind == JsonValueKind.Object && saves.TryGetProperty(saveKey, out var save))
                stats.SaveRanks[saveKey] = MapRank(GetNestedInt(save, "rank"));
        }

        if (skills.ValueKind == JsonValueKind.Object)
        {
            foreach (var (key, _) in Pf2eLookups.Skills)
            {
                if (!skills.TryGetProperty(key, out var skill)) continue;
                var rank = MapRank(GetNestedInt(skill, "rank"));
                if (rank > 0) stats.SkillRanks[key] = rank;
            }
        }

        foreach (var item in items)
        {
            var itemType = GetString(item, "type");
            if (itemType is null) continue;

            switch (itemType)
            {
                case "feat":
                    ParseFeat(item, stats);
                    break;
                case "weapon":
                    ParseWeapon(item, stats, proficiencies, level, str, dex);
                    break;
                case "armor":
                case "shield":
                    ParseArmor(item, stats);
                    break;
                case "equipment":
                case "consumable":
                case "backpack":
                case "treasure":
                    ParseEquipment(item, stats);
                    break;
                case "spell":
                    ParseSpell(item, stats);
                    break;
                case "spellcastingEntry":
                    ParseSpellcastingEntry(item, stats);
                    break;
            }
        }

        var focusMax = GetNestedInt(resources, "focus", "max");
        var focusValue = GetNestedInt(resources, "focus", "value");
        if (focusMax > 0)
            stats.Resources.Add(new Pf2eLookups.Pf2eResource("Focus Points", focusValue, focusMax));

        var featureLines = new List<string>();
        if (ItemName(heritageItem) is { } heritageName)
            featureLines.Add(heritageName);

        var equipmentNames = stats.Inventory
            .Select(i => i.Quantity > 1 ? $"{i.Name} ×{i.Quantity}" : i.Name)
            .ToList();

        var request = new ImportCharacterRequest(
            Name: name,
            Race: ItemName(ancestryItem) ?? GetNestedString(details, "ancestry", "name") ?? "—",
            Class: ItemName(classItem) ?? GetNestedString(details, "class", "name") ?? "—",
            Level: level,
            Background: ItemName(backgroundItem) ?? GetNestedString(details, "background", "name"),
            Alignment: GetNestedString(details, "alignment", "value") ?? GetString(details, "alignment"),
            Strength: str, Dexterity: dex, Constitution: con,
            Intelligence: intScore, Wisdom: wis, Charisma: cha,
            MaxHitPoints: maxHp, CurrentHitPoints: currentHp,
            ArmorClass: ac, Speed: speed,
            HitDice: $"{level}d{classHpDie}",
            SkillProficiencies: Pf2eLookups.Skills
                .Where(s => stats.SkillRanks.GetValueOrDefault(s.Key) > 0)
                .Select(s => s.Label).ToList(),
            SavingThrowProficiencies: [],
            FeaturesAndTraits: featureLines.Count > 0 ? string.Join("\n", featureLines) : null,
            Equipment: equipmentNames.Count > 0 ? string.Join("\n", equipmentNames) : null);

        return (request, stats);
    }

    private static int MapRank(int foundryRank) => Math.Clamp(foundryRank, 0, 4) * 2;

    private static int ResolveClassDcRank(JsonElement proficiencies)
    {
        if (proficiencies.ValueKind != JsonValueKind.Object
            || !proficiencies.TryGetProperty("classDCs", out var dcs)
            || dcs.ValueKind != JsonValueKind.Object)
            return 0;

        foreach (var prop in dcs.EnumerateObject())
        {
            if (prop.Value.ValueKind != JsonValueKind.Object) continue;
            if (prop.Value.TryGetProperty("primary", out var primary) && primary.ValueKind == JsonValueKind.True)
                return MapRank(GetNestedInt(prop.Value, "rank"));
        }

        foreach (var prop in dcs.EnumerateObject())
        {
            if (prop.Value.ValueKind != JsonValueKind.Object) continue;
            var rank = MapRank(GetNestedInt(prop.Value, "rank"));
            if (rank > 0) return rank;
        }

        return 0;
    }

    private static int ResolveSpeed(JsonElement system, JsonElement attributes)
    {
        var land = GetNestedInt(system, "movement", "speeds", "land", "value");
        if (land > 0) return land;
        land = GetNestedInt(attributes, "speed", "value");
        if (land > 0) return land;
        if (attributes.ValueKind == JsonValueKind.Object
            && attributes.TryGetProperty("speed", out var speedEl) && speedEl.ValueKind == JsonValueKind.Number)
            return speedEl.GetInt32();
        return 25;
    }

    private static void ParseFeat(JsonElement item, Pf2eLookups.Pf2eStatsModel stats)
    {
        var featName = GetString(item, "name");
        if (string.IsNullOrWhiteSpace(featName)) return;
        var system = item.TryGetProperty("system", out var sys) && sys.ValueKind == JsonValueKind.Object ? sys : default;
        var featLevel = GetNestedInt(system, "level", "value", 1);
        var slug = GetNestedString(system, "slug") ?? Pf2eLookups.SlugifyItemName(featName);
        stats.Feats.Add(new Pf2eLookups.Pf2eFeat(featName, featLevel, slug));
    }

    private static void ParseWeapon(
        JsonElement item, Pf2eLookups.Pf2eStatsModel stats, JsonElement proficiencies, int level, int str, int dex)
    {
        if (!IsEquipped(item)) return;

        var weaponName = GetString(item, "name");
        if (string.IsNullOrWhiteSpace(weaponName)) return;

        var system = item.TryGetProperty("system", out var sys) && sys.ValueKind == JsonValueKind.Object ? sys : default;
        var slug = GetNestedString(system, "slug") ?? Pf2eLookups.SlugifyItemName(weaponName);
        stats.Inventory.Add(new Pf2eLookups.Pf2eInventoryItem(weaponName, 1, 0, true, slug));

        var category = GetNestedString(system, "category") ?? "simple";
        var weaponRank = ResolveAttackRank(proficiencies, category);
        var damage = system.TryGetProperty("damage", out var dmg) && dmg.ValueKind == JsonValueKind.Object ? dmg : default;
        var die = GetNestedString(damage, "die") ?? "d6";
        var striking = system.TryGetProperty("runes", out var runes) && runes.ValueKind == JsonValueKind.Object
            ? GetNestedInt(runes, "striking")
            : 0;
        var diceCount = Math.Max(1, GetInt(damage, "dice", 1) + striking);
        var damageBonus = GetNestedInt(damage, "modifier");
        var damageType = GetNestedString(damage, "damageType");

        var abilityKey = GetNestedString(system, "attribute") ?? "str";
        if (abilityKey is not ("str" or "dex"))
            abilityKey = "str";

        var potency = system.TryGetProperty("runes", out runes) && runes.ValueKind == JsonValueKind.Object
            ? GetNestedInt(runes, "potency")
            : 0;
        var fixedAttack = GetNestedInt(item, "flags", "pf2e", "fixedAttack");
        if (fixedAttack != 0)
        {
            var targetBonus = fixedAttack - potency;
            var baseBonus = Pf2eLookups.Bonus(weaponRank, level);
            abilityKey = targetBonus - baseBonus == (dex - 10) / 2 && dex != str ? "dex" : "str";
        }

        var traits = system.TryGetProperty("traits", out var traitsEl) && traitsEl.TryGetProperty("value", out var traitsVal)
            ? traitsVal.EnumerateArray().Select(t => t.GetString()?.ToLowerInvariant() ?? "").Where(t => t.Length > 0).ToList()
            : [];
        int? equipmentRange = null;
        if (system.TryGetProperty("range", out var rangeEl))
        {
            if (rangeEl.ValueKind == JsonValueKind.Number)
                equipmentRange = rangeEl.GetInt32();
            else if (rangeEl.TryGetProperty("increment", out var inc) && inc.TryGetInt32(out var incVal))
                equipmentRange = incVal;
        }

        var (rangeFeet, reachFeet) = Pf2eLookups.ParseWeaponRangeFromTraits(traits, equipmentRange);

        stats.Attacks.Add(new Pf2eLookups.Pf2eAttack(
            weaponName, weaponRank, abilityKey,
            $"{diceCount}{die}", damageBonus, damageType, rangeFeet, reachFeet));
    }

    private static void ParseArmor(JsonElement item, Pf2eLookups.Pf2eStatsModel stats)
    {
        var armorName = GetString(item, "name");
        if (string.IsNullOrWhiteSpace(armorName)) return;
        var system = item.TryGetProperty("system", out var sys) && sys.ValueKind == JsonValueKind.Object ? sys : default;
        var slug = GetNestedString(system, "slug") ?? Pf2eLookups.SlugifyItemName(armorName);
        var equipped = IsEquipped(item);
        stats.Inventory.Add(new Pf2eLookups.Pf2eInventoryItem(armorName, 1, 0, equipped, slug));
    }

    private static void ParseEquipment(JsonElement item, Pf2eLookups.Pf2eStatsModel stats)
    {
        var itemName = GetString(item, "name");
        if (string.IsNullOrWhiteSpace(itemName)) return;
        var system = item.TryGetProperty("system", out var sys) && sys.ValueKind == JsonValueKind.Object ? sys : default;
        var slug = GetNestedString(system, "slug") ?? Pf2eLookups.SlugifyItemName(itemName);
        var qty = Math.Max(1, GetInt(system, "quantity", 1));
        var equipped = IsEquipped(item);
        stats.Inventory.Add(new Pf2eLookups.Pf2eInventoryItem(itemName, qty, 0, equipped, slug));
    }

    private static void ParseSpell(JsonElement item, Pf2eLookups.Pf2eStatsModel stats)
    {
        var spellName = GetString(item, "name");
        if (string.IsNullOrWhiteSpace(spellName)) return;
        var system = item.TryGetProperty("system", out var sys) && sys.ValueKind == JsonValueKind.Object ? sys : default;
        var spellLevel = GetNestedInt(system, "level", "value");
        stats.KnownSpells.Add(new Pf2eLookups.Pf2eKnownSpell(spellName, spellLevel, Prepared: false));
    }

    private static void ParseSpellcastingEntry(JsonElement item, Pf2eLookups.Pf2eStatsModel stats)
    {
        if (stats.SpellcastingTradition is not null) return;

        var system = item.TryGetProperty("system", out var sys) && sys.ValueKind == JsonValueKind.Object ? sys : default;
        stats.SpellcastingTradition = GetNestedString(system, "tradition", "value");
        stats.SpellcastingRank = MapRank(GetNestedInt(system, "proficiency", "value"));

        if (system.ValueKind != JsonValueKind.Object || !system.TryGetProperty("slots", out var slots)
            || slots.ValueKind != JsonValueKind.Object)
            return;

        foreach (var slotProp in slots.EnumerateObject())
        {
            if (!slotProp.Name.StartsWith("slot", StringComparison.Ordinal)) continue;
            if (!int.TryParse(slotProp.Name.AsSpan(4), out var slotLevel)) continue;
            if (slotProp.Value.ValueKind != JsonValueKind.Object) continue;
            var max = GetNestedInt(slotProp.Value, "max");
            if (max > 0)
                stats.SpellSlots[slotLevel] = new Pf2eLookups.Pf2eSpellSlotLevel(max, 0);
        }
    }

    private static int ResolveAttackRank(JsonElement proficiencies, string category)
    {
        if (proficiencies.ValueKind == JsonValueKind.Object
            && proficiencies.TryGetProperty("attacks", out var attacks)
            && attacks.ValueKind == JsonValueKind.Object)
        {
            if (attacks.TryGetProperty(category, out var cat) && cat.ValueKind == JsonValueKind.Object)
                return MapRank(GetNestedInt(cat, "rank"));
            var groupKey = $"weapon-group-{category}";
            if (attacks.TryGetProperty(groupKey, out var group) && group.ValueKind == JsonValueKind.Object)
                return MapRank(GetNestedInt(group, "rank"));
        }

        return category switch
        {
            "martial" or "advanced" => 2,
            "simple" or "unarmed" => 2,
            _ => 0,
        };
    }

    private static bool IsEquipped(JsonElement item)
    {
        if (!item.TryGetProperty("system", out var system) || system.ValueKind != JsonValueKind.Object)
            return false;
        if (!system.TryGetProperty("equipped", out var equipped) || equipped.ValueKind != JsonValueKind.Object)
            return false;
        var carryType = GetNestedString(equipped, "carryType");
        return carryType is "held" or "worn";
    }

    private static JsonElement? FindItem(List<JsonElement> items, string type)
    {
        foreach (var item in items)
        {
            if (GetString(item, "type") == type) return item;
        }
        return null;
    }

    private static string? ItemName(JsonElement? item) =>
        item is { } el ? GetString(el, "name") : null;

    private static IEnumerable<JsonElement> EnumerateItems(JsonElement root)
    {
        if (!root.TryGetProperty("items", out var items)) yield break;
        switch (items.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in items.EnumerateArray()) yield return item;
                break;
            case JsonValueKind.Object:
                foreach (var prop in items.EnumerateObject()) yield return prop.Value;
                break;
        }
    }

    private static string? GetString(JsonElement el, string prop) =>
        el.ValueKind == JsonValueKind.Object && el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static string? GetNestedString(JsonElement el, params string[] path)
    {
        var current = el;
        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
                return null;
        }
        return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
    }

    private static int GetNestedInt(JsonElement el, params string[] path)
    {
        var lastIndex = path.Length - 1;
        var current = el;
        for (var i = 0; i < path.Length; i++)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(path[i], out current))
                return 0;
            if (i == lastIndex && current.ValueKind == JsonValueKind.Number)
                return current.GetInt32();
        }
        return 0;
    }

    private static int GetNestedInt(JsonElement el, string prop1, string prop2, int defaultValue)
    {
        if (el.ValueKind != JsonValueKind.Object || !el.TryGetProperty(prop1, out var child))
            return defaultValue;
        if (child.ValueKind == JsonValueKind.Number) return child.GetInt32();
        if (child.ValueKind == JsonValueKind.Object && child.TryGetProperty(prop2, out var value)
            && value.ValueKind == JsonValueKind.Number)
            return value.GetInt32();
        return defaultValue;
    }

    private static int GetInt(JsonElement el, string prop, int fallback) =>
        el.ValueKind == JsonValueKind.Object && el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetInt32()
            : fallback;
}
