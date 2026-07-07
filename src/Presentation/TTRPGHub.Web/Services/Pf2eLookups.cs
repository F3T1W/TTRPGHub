using System.Text.Json;

namespace TTRPGHub.Services;

// Общие PF2e-справочные списки и модель PF2e-листа персонажа (Character.Pf2eStatsJson) —
// используются и на листе персонажа (Pages/Characters/Detail.razor.cs), и за игровым столом
// (Pages/Sessions/Table.razor.cs, для автоподстановки бонуса проверки из ранга владения).
public static class Pf2eLookups
{
    // J.6 — у монстров нет ссылок на реальные иконки (лицензия Foundry на арт не позволяет
    // переиспользовать их иконки существ), поэтому вместо пустого токена подставляем
    // плейсхолдер-арт по типу существа, определяемому по трейтам из статблока.
    private static readonly (string Trait, string Icon)[] CreatureTypeIcons =
    [
        ("dragon", "dragon"), ("undead", "undead"), ("humanoid", "humanoid"),
        ("animal", "animal"), ("beast", "animal"), ("aberration", "aberration"),
        ("construct", "construct"), ("elemental", "elemental"), ("fiend", "fiend"),
        ("celestial", "celestial"), ("plant", "plant"), ("ooze", "ooze"),
        ("fungus", "fungus"), ("giant", "giant"),
    ];

    public static string MonsterPlaceholderIcon(string? traits)
    {
        if (!string.IsNullOrWhiteSpace(traits))
        {
            var traitList = traits.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            foreach (var (trait, icon) in CreatureTypeIcons)
                if (traitList.Any(t => t.Equals(trait, StringComparison.OrdinalIgnoreCase)))
                    return $"/img/tokens/{icon}.svg";
        }

        return "/img/tokens/generic.svg";
    }

    public static readonly (string Key, string Label)[] Abilities =
    [
        ("str", "Сила"), ("dex", "Ловкость"), ("con", "Телосложение"),
        ("int", "Интеллект"), ("wis", "Мудрость"), ("cha", "Харизма")
    ];

    public static readonly (string Key, string Label)[] Saves =
    [
        ("fortitude", "Стойкость"), ("reflex", "Реакция"), ("will", "Воля")
    ];

    public static readonly (string Key, string Label)[] Skills =
    [
        ("acrobatics", "Акробатика"), ("arcana", "Магия"), ("athletics", "Атлетика"),
        ("crafting", "Ремесло"), ("deception", "Обман"), ("diplomacy", "Дипломатия"),
        ("intimidation", "Запугивание"), ("lore", "Знания"), ("medicine", "Медицина"),
        ("nature", "Природа"), ("occultism", "Оккультизм"), ("performance", "Выступление"),
        ("religion", "Религия"), ("society", "Общество"), ("stealth", "Скрытность"),
        ("survival", "Выживание"), ("thievery", "Воровство")
    ];

    public static readonly (int Value, string Label)[] Ranks =
    [
        (0, "Неопытный"), (2, "Обучен"), (4, "Эксперт"), (6, "Мастер"), (8, "Легенда")
    ];

    // Untrained в PF2e не получает бонус уровня — только Trained и выше.
    public static int Bonus(int rank, int level) => rank == 0 ? 0 : rank + level;

    // L.1 — MAP: strikeIndex 0 = первая атака в ходу, 1 = вторая, 2+ = третья и далее.
    public static int MapPenalty(int strikeIndex, bool agile = false) => strikeIndex switch
    {
        <= 0 => 0,
        1 => agile ? -4 : -5,
        _ => agile ? -8 : -10
    };

    // L.2 — бонус атаки заклинанием и DC (классовая формула PF2e: 10 + владение + мод. хар-ки).
    public static int SpellAttackBonus(int spellcastingRank, int level, int abilityMod) =>
        Bonus(spellcastingRank, level) + abilityMod;

    public static int SpellDc(int spellcastingRank, int level, int abilityMod) =>
        10 + Bonus(spellcastingRank, level) + abilityMod;

    // L.7 — DC врождённых способностей монстра: 10 + уровень/2 + лучший ментальный мод.
    public static int MonsterAbilityDc(int level, int intelligence, int wisdom, int charisma)
    {
        var mental = Math.Max(AbilityMod(intelligence), Math.Max(AbilityMod(wisdom), AbilityMod(charisma)));
        return 10 + level / 2 + mental;
    }

    private static int AbilityMod(int score) => (score - 10) / 2;

    public sealed record Pf2eInventoryItem(string Name, int Quantity, double Bulk, bool Equipped, string? Slug = null);

    // Не полноценное структурированное оружие (нет рун/трейтов/групп) — сознательно упрощённая
    // "запись атаки": ранг владения + характеристика для to-hit, кость и бонус урона. Этого
    // достаточно для автоматизации броска атаки/урона за столом (H.7), не блокируясь на полном
    // моделировании оружейной системы PF2e.
    public sealed record Pf2eAttack(string Name, int Rank, string AbilityKey, string DamageDice, int DamageBonus, string? DamageType);

    // Атака монстра (Pf2eMonster.AttacksJson, см. I.2) — в отличие от Pf2eAttack персонажа,
    // бонус атаки здесь уже готовое число из статблока, не считается из ранга+уровня+характеристики.
    public sealed record Pf2eMonsterAttack(string Name, int Bonus, string DamageDice, int DamageBonus, string? DamageType);

    public static List<Pf2eMonsterAttack> ParseMonsterAttacks(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<Pf2eMonsterAttack>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? []; }
        catch { return []; }
    }

    // Сопротивление/уязвимость монстра (Pf2eMonster.ResistancesJson/WeaknessesJson, J.2) — value
    // всегда положительное число по правилам PF2e, знак применяется при расчёте (сопротивление
    // вычитает, уязвимость добавляет). Exceptions — типы урона/трейты атаки, которые "пробивают"
    // сопротивление (например physical 5 (except magical) — магическая физическая атака игнорирует
    // сопротивление); для уязвимостей exceptions в данных Foundry не встречаются, но поле общее.
    public sealed record Pf2eDamageAdjustment(string Type, int Value, List<string> Exceptions);

    public static List<Pf2eDamageAdjustment> ParseDamageAdjustments(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<Pf2eDamageAdjustment>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? []; }
        catch { return []; }
    }

    // Эффективный урон с учётом одного применимого сопротивления/уязвимости монстра по
    // выбранному игроком типу урона. Не пытается сопоставить "except"-трейты автоматически
    // (нет структурированных трейтов входящей атаки в системе) — GM видит exceptions в подписи
    // и сам решает, применимо ли сопротивление к конкретному удару (см. UI в Table.razor).
    public static int ApplyDamageAdjustment(int rawDamage, int? resistance, int? weakness)
    {
        var effective = rawDamage;
        if (resistance is { } r) effective = Math.Max(0, effective - r);
        if (weakness is { } w) effective += w;
        return effective;
    }

    // Структурированный фит: имя, уровень, и опционально Slug — связь с записью в общем
    // справочнике RuleEntry (Feat). Slug заполняется, только если фит выбран из автодополнения
    // по каталогу (см. Detail.razor "datalist"); свободный текст без совпадения по каталогу
    // остаётся с Slug = null и не участвует в автоматизации правил (J.1) — просто текст, как раньше.
    public sealed record Pf2eFeat(string Name, int Level, string? Slug = null);

    // Числовой модификатор из данных фита (J.1) — селектор: "land-speed", ключ навыка,
    // "perception" или "ac". Predicate (может быть null = безусловный) — сырое PF2e-условие
    // из Foundry: массив строк (все должны быть истинны) и/или объектов {or,not,nor,nand,gte,
    // lte,gt,lt} — вычисляется PredicateEvaluator против статичных фактов персонажа.
    public sealed record Pf2eFlatModifier(string Selector, int Value, string Type, JsonElement? Predicate);

    public sealed record Pf2eFeatStats(List<Pf2eFlatModifier> Modifiers, List<string>? Grants);

    public static List<Pf2eFlatModifier> ParseFeatModifiers(string? statsJson)
    {
        if (string.IsNullOrWhiteSpace(statsJson)) return [];
        try
        {
            var stats = JsonSerializer.Deserialize<Pf2eFeatStats>(statsJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return stats?.Modifiers ?? [];
        }
        catch { return []; }
    }

    // L.4 — контекст экипировки для roll options item:* / armor:* и модификаторов из stats_json.
    public sealed record EquippedItemContext(
        string Slug, string? ItemKind, string? ArmorCategory,
        IReadOnlyList<string> Traits, bool IsRanged, string? DamageCategory);

    public sealed record AncestryRollContext(string? Slug, IReadOnlyList<string> Traits, int? Size);

    private static readonly Dictionary<string, string> AncestrySlugByDisplayName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Человек"] = "human", ["Дварф"] = "dwarf", ["Эльф"] = "elf", ["Полурослик"] = "halfling",
        ["Гном"] = "gnome", ["Полуорк"] = "half-orc", ["Полуэльф"] = "half-elf", ["Гоблин"] = "goblin",
        ["Орк"] = "orc", ["Тифлинг"] = "tiefling", ["Гнолл"] = "gnoll", ["Хобгоблин"] = "hobgoblin",
    };

    private static readonly Dictionary<string, string[]> AncestryTraitsBySlug = new(StringComparer.OrdinalIgnoreCase)
    {
        ["human"] = ["human", "humanoid"], ["dwarf"] = ["dwarf", "humanoid"], ["elf"] = ["elf", "humanoid"],
        ["halfling"] = ["halfling", "humanoid"], ["gnome"] = ["gnome", "humanoid"],
        ["half-orc"] = ["human", "orc", "humanoid"], ["half-elf"] = ["human", "elf", "humanoid"],
        ["goblin"] = ["goblin", "humanoid"], ["orc"] = ["orc", "humanoid"],
        ["tiefling"] = ["tiefling", "humanoid"], ["gnoll"] = ["gnoll", "humanoid"],
        ["hobgoblin"] = ["hobgoblin", "humanoid"],
    };

    private static readonly Dictionary<string, int> AncestrySizeBySlug = new(StringComparer.OrdinalIgnoreCase)
    {
        ["halfling"] = 1, ["gnome"] = 1, ["goblin"] = 1,
        ["human"] = 2, ["dwarf"] = 2, ["elf"] = 2, ["half-orc"] = 2, ["half-elf"] = 2,
        ["orc"] = 2, ["tiefling"] = 2, ["gnoll"] = 2, ["hobgoblin"] = 2,
    };

    public static readonly (string Slug, string Label)[] SceneTerrainTags =
    [
        ("forest", "Лес"), ("wilderness", "Дикая местность"), ("snow", "Снег"),
        ("unusual-stonework", "Необычная кладка"),
    ];

    public static readonly (string Slug, string Label)[] SceneAmbientLighting =
    [
        ("bright", "Яркий свет"), ("dim-light", "Полумрак"), ("darkness", "Темнота"),
    ];

    public static string SlugifyItemName(string name) =>
        string.Join('-', name.Trim().ToLowerInvariant().Split([' ', '_'], StringSplitOptions.RemoveEmptyEntries));

    public static string? ResolveAncestrySlug(string? raceDisplayName)
    {
        if (string.IsNullOrWhiteSpace(raceDisplayName)) return null;
        var trimmed = raceDisplayName.Trim();
        if (AncestrySlugByDisplayName.TryGetValue(trimmed, out var slug)) return slug;
        return SlugifyItemName(trimmed);
    }

    public static IReadOnlyList<string> AncestryTraits(string? ancestrySlug) =>
        ancestrySlug is null ? [] : AncestryTraitsBySlug.GetValueOrDefault(ancestrySlug, ["humanoid"]);

    public static AncestryRollContext BuildAncestryRollContext(string? raceDisplayName, string? raceStatsJson = null)
    {
        var slug = ResolveAncestrySlug(raceDisplayName);
        var traits = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int? size = null;

        if (!string.IsNullOrWhiteSpace(raceStatsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(raceStatsJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("traits", out var traitsEl) && traitsEl.ValueKind == JsonValueKind.Array)
                    foreach (var t in traitsEl.EnumerateArray())
                    {
                        var s = t.GetString();
                        if (!string.IsNullOrWhiteSpace(s)) traits.Add(s.ToLowerInvariant());
                    }
                if (root.TryGetProperty("size", out var sizeEl) && sizeEl.TryGetInt32(out var sizeVal))
                    size = sizeVal;
            }
            catch { /* fallback ниже */ }
        }

        if (slug is not null)
        {
            foreach (var t in AncestryTraits(slug)) traits.Add(t);
            size ??= AncestrySizeBySlug.GetValueOrDefault(slug, 2);
        }

        return new AncestryRollContext(slug, traits.ToList(), size);
    }

    public static List<string> ParseTerrainTags(string? terrainTagsJson)
    {
        if (string.IsNullOrWhiteSpace(terrainTagsJson)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(terrainTagsJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))?
                .Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim().ToLowerInvariant()).ToList() ?? [];
        }
        catch { return []; }
    }

    public static IReadOnlyList<string> ParseEquipmentTraits(JsonElement root)
    {
        if (!root.TryGetProperty("traits", out var traitsEl)) return [];
        if (traitsEl.ValueKind == JsonValueKind.Array)
            return traitsEl.EnumerateArray().Select(t => t.GetString()?.Trim().ToLowerInvariant())
                .Where(t => !string.IsNullOrEmpty(t)).Cast<string>().ToList();
        if (traitsEl.ValueKind == JsonValueKind.String)
            return traitsEl.GetString()?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.ToLowerInvariant()).ToList() ?? [];
        return [];
    }

    public static EquippedItemContext ParseEquipmentContext(string slug, string? statsJson)
    {
        if (string.IsNullOrWhiteSpace(statsJson)) return new EquippedItemContext(slug, null, null, [], false, null);
        try
        {
            using var doc = JsonDocument.Parse(statsJson);
            var root = doc.RootElement;
            var kind = root.TryGetProperty("item_kind", out var k) ? k.GetString() : null;
            string? armorCat = null;
            bool isRanged = false;
            string? damageCategory = null;
            if (root.TryGetProperty("extra", out var extra))
            {
                if (extra.TryGetProperty("category", out var cat))
                    armorCat = cat.GetString();
                if (extra.TryGetProperty("range", out var range) && range.ValueKind != JsonValueKind.Null)
                {
                    if (range.ValueKind == JsonValueKind.Number && range.GetInt32() > 0) isRanged = true;
                    if (range.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(range.GetString())) isRanged = true;
                }
                if (extra.TryGetProperty("damage_type", out var dt))
                    damageCategory = MapDamageCategory(dt.GetString());
            }

            var traits = ParseEquipmentTraits(root);
            if (traits.Contains("ranged") || traits.Contains("thrown")) isRanged = true;

            return new EquippedItemContext(slug, kind, armorCat, traits, isRanged, damageCategory);
        }
        catch { return new EquippedItemContext(slug, null, null, [], false, null); }
    }

    private static string? MapDamageCategory(string? damageType) => damageType?.ToLowerInvariant() switch
    {
        "slashing" or "piercing" or "bludgeoning" or "physical" => "physical",
        _ => damageType?.ToLowerInvariant(),
    };

    public static List<Pf2eFlatModifier> ParseItemModifiers(string? statsJson)
    {
        var mods = ParseFeatModifiers(statsJson);
        if (string.IsNullOrWhiteSpace(statsJson)) return mods;
        try
        {
            using var doc = JsonDocument.Parse(statsJson);
            var root = doc.RootElement;
            var kind = root.TryGetProperty("item_kind", out var k) ? k.GetString() : null;
            if (kind is not ("armor" or "shield")) return mods;
            if (!root.TryGetProperty("extra", out var extra)
                || !extra.TryGetProperty("ac_bonus", out var ac)
                || !ac.TryGetInt32(out var acBonus)
                || acBonus == 0)
                return mods;
            mods.Add(new Pf2eFlatModifier("ac", acBonus, "item", null));
        }
        catch { /* без синтетического бонуса доспеха */ }
        return mods;
    }

    public static void AddEquippedItemRollOptions(
        HashSet<string> options, IEnumerable<Pf2eInventoryItem> inventory, IEnumerable<EquippedItemContext> meta)
    {
        var metaBySlug = meta.ToDictionary(m => m.Slug, StringComparer.OrdinalIgnoreCase);
        var hasArmor = false;
        foreach (var item in inventory.Where(i => i.Equipped))
        {
            var slug = item.Slug ?? SlugifyItemName(item.Name);
            if (string.IsNullOrEmpty(slug)) continue;
            options.Add($"item:{slug}");
            if (!metaBySlug.TryGetValue(slug, out var ctx))
                continue;

            foreach (var trait in ctx.Traits)
            {
                options.Add($"item:trait:{trait}");
                if (trait is "magical" or "divine" or "holy")
                    options.Add($"item:{trait}");
            }

            if (ctx.IsRanged) options.Add("item:ranged");
            if (ctx.Traits.Contains("detection")) options.Add("item:trait:detection");
            if (ctx.DamageCategory is "physical") options.Add("item:damage:category:physical");

            if (ctx.ItemKind is "armor" or "shield")
            {
                hasArmor = true;
                options.Add($"armor:slug:{slug}");
                if (!string.IsNullOrEmpty(ctx.ArmorCategory))
                    options.Add($"armor:category:{ctx.ArmorCategory}");
            }
        }

        if (hasArmor)
        {
            options.Add("armor:equipped");
            options.Add("self:armored");
        }
    }

    public static void AddAncestryRollOptions(HashSet<string> options, AncestryRollContext ancestry)
    {
        if (ancestry.Slug is { } slug)
            options.Add($"self:ancestry:{slug}");
        foreach (var trait in ancestry.Traits)
            options.Add($"self:trait:{trait}");
        if (ancestry.Size is { } size)
            options.Add($"self:size:{size}");
    }

    public static void AddSceneEnvironmentRollOptions(
        HashSet<string> options, IEnumerable<string> terrainTags, string ambientLighting)
    {
        foreach (var tag in terrainTags)
            options.Add($"terrain:{tag}");
        if (!string.Equals(ambientLighting, "bright", StringComparison.OrdinalIgnoreCase))
            options.Add($"lighting:{ambientLighting.ToLowerInvariant()}");
    }

    public static List<string> ParseFeatGrants(string? statsJson)
    {
        if (string.IsNullOrWhiteSpace(statsJson)) return [];
        try
        {
            var stats = JsonSerializer.Deserialize<Pf2eFeatStats>(statsJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return stats?.Grants ?? [];
        }
        catch { return []; }
    }

    // K.2 — действия навыков PF2e для контекста проверки за столом: выбранное действие даёт
    // roll option "action:{slug}", от которого зависят условные модификаторы фитов («+1 к Обману,
    // когда Лжёте» и т.п.). Список — действия, реально встречающиеся в предикатах импортированных
    // фитов (45 слагов), сгруппирован по навыку для удобства выбора.
    public static readonly (string Slug, string Label)[] SkillActions =
    [
        ("lie", "Солгать"), ("impersonate", "Выдать себя за другого"),
        ("make-an-impression", "Произвести впечатление"), ("gather-information", "Собрать сведения"),
        ("request", "Попросить"), ("coerce", "Принудить"), ("demoralize", "Запугать"),
        ("grapple", "Схватить"), ("shove", "Толкнуть"), ("trip", "Сбить с ног"),
        ("reposition", "Переместить"), ("escape", "Вырваться"), ("force-open", "Взломать силой"),
        ("climb", "Взобраться"), ("high-jump", "Прыжок в высоту"), ("long-jump", "Прыжок в длину"),
        ("lift-heavy-object", "Поднять тяжесть"), ("hide", "Спрятаться"), ("sneak", "Красться"),
        ("conceal-an-object", "Спрятать предмет"), ("palm-an-object", "Спрятать в ладони"),
        ("steal", "Украсть"), ("pick-a-lock", "Вскрыть замок"), ("disable-a-device", "Обезвредить устройство"),
        ("tumble-through", "Проскользнуть"), ("maneuver-in-flight", "Манёвр в полёте"),
        ("squeeze", "Протиснуться"), ("seek", "Осмотреться"), ("sense-motive", "Распознать мотивы"),
        ("sense-direction", "Определить направление"), ("recall-knowledge", "Вспомнить знания"),
        ("identify-magic", "Опознать магию"), ("treat-wounds", "Обработать раны"),
        ("treat-disease", "Лечить болезнь"), ("administer-first-aid", "Оказать первую помощь"),
        ("command-an-animal", "Приказать животному"), ("perform", "Выступить"),
        ("craft", "Изготовить"), ("repair", "Починить"), ("earn-income", "Заработать"),
    ];

    // K.2 — модификатор от состояния (frightened −X ко всем проверкам и т.п.), распарсенный из
    // StatsJson условия в rule_entries: selectors — к каким проверкам применяется ("all" = ко
    // всем), kind: "flat" (готовое значение) или "per_badge" (значение состояния × множитель,
    // например frightened 2 × −1 = −2); "unsupported" — описательные эффекты, пропускаются.
    public sealed record Pf2eConditionModifier(List<string> Selectors, string Type, string Kind, int Amount);

    public static List<Pf2eConditionModifier> ParseConditionModifiers(string? statsJson)
    {
        if (string.IsNullOrWhiteSpace(statsJson)) return [];
        try
        {
            using var doc = JsonDocument.Parse(statsJson);
            if (!doc.RootElement.TryGetProperty("modifiers", out var mods) || mods.ValueKind != JsonValueKind.Array)
                return [];

            var result = new List<Pf2eConditionModifier>();
            foreach (var m in mods.EnumerateArray())
            {
                if (!m.TryGetProperty("formula", out var f) || f.ValueKind != JsonValueKind.Object) continue;
                var kind = f.TryGetProperty("kind", out var k) ? k.GetString() ?? "unsupported" : "unsupported";
                if (kind == "unsupported") continue;

                var amount = kind == "per_badge"
                    ? (f.TryGetProperty("multiplier", out var mult) ? mult.GetInt32() : 0)
                    : (f.TryGetProperty("value", out var val) ? val.GetInt32() : 0);
                var selectors = m.TryGetProperty("selectors", out var sel) && sel.ValueKind == JsonValueKind.Array
                    ? sel.EnumerateArray().Select(s => s.GetString() ?? "").Where(s => s.Length > 0).ToList()
                    : [];
                var type = m.TryGetProperty("type", out var t) ? t.GetString() ?? "untyped" : "untyped";
                result.Add(new Pf2eConditionModifier(selectors, type, kind, amount));
            }

            return result;
        }
        catch { return []; }
    }

    // Правило стекинга PF2e: бонусы одного типа (status/item/circumstance) не складываются —
    // берётся лучший; штрафы одного типа — худший. Бонус и штраф одного типа сосуществуют.
    // Нетипизированные (untyped) складываются свободно.
    public static int StackModifiers(IEnumerable<(string Type, int Value)> modifiers)
    {
        var total = 0;
        foreach (var group in modifiers.GroupBy(m => m.Type))
        {
            if (group.Key == "untyped")
            {
                total += group.Sum(m => m.Value);
                continue;
            }

            var positives = group.Where(m => m.Value > 0).Select(m => m.Value).ToList();
            var negatives = group.Where(m => m.Value < 0).Select(m => m.Value).ToList();
            if (positives.Count > 0) total += positives.Max();
            if (negatives.Count > 0) total += negatives.Min();
        }

        return total;
    }

    // Вычислитель предикатов PF2e на статичном листе персонажа (J.1, предел без combat tracker).
    // Термы, которые нельзя вычислить без контекста конкретного броска/цели/окружения (J.2+),
    // безопасно считаются ложными — модификатор просто не применяется, а не применяется неверно.
    public static class PredicateEvaluator
    {
        public static bool Evaluate(JsonElement? predicate, HashSet<string> rollOptions, Dictionary<string, double> facts)
        {
            if (predicate is null || predicate.Value.ValueKind != JsonValueKind.Array)
                return true;

            foreach (var term in predicate.Value.EnumerateArray())
            {
                if (!EvaluateTerm(term, rollOptions, facts))
                    return false;
            }
            return true;
        }

        private static bool EvaluateTerm(JsonElement term, HashSet<string> rollOptions, Dictionary<string, double> facts)
        {
            if (term.ValueKind == JsonValueKind.String)
                return rollOptions.Contains(term.GetString() ?? "");

            if (term.ValueKind != JsonValueKind.Object)
                return false;

            foreach (var prop in term.EnumerateObject())
            {
                switch (prop.Name)
                {
                    case "or":
                        if (!prop.Value.EnumerateArray().Any(t => EvaluateTerm(t, rollOptions, facts))) return false;
                        break;
                    case "and":
                        if (!prop.Value.EnumerateArray().All(t => EvaluateTerm(t, rollOptions, facts))) return false;
                        break;
                    case "not":
                        if (prop.Value.EnumerateArray().Any(t => EvaluateTerm(t, rollOptions, facts))) return false;
                        break;
                    case "nor":
                        if (prop.Value.EnumerateArray().Any(t => EvaluateTerm(t, rollOptions, facts))) return false;
                        break;
                    case "nand":
                        if (prop.Value.EnumerateArray().All(t => EvaluateTerm(t, rollOptions, facts))) return false;
                        break;
                    case "gte": case "lte": case "gt": case "lt":
                        if (!EvaluateComparison(prop.Name, prop.Value, facts)) return false;
                        break;
                    default:
                        // Неизвестный оператор — безопасно считаем условие невыполненным.
                        return false;
                }
            }
            return true;
        }

        private static bool EvaluateComparison(string op, JsonElement pair, Dictionary<string, double> facts)
        {
            if (pair.ValueKind != JsonValueKind.Array || pair.GetArrayLength() != 2)
                return false;

            var items = pair.EnumerateArray().ToList();
            if (!TryResolveOperand(items[0], facts, out var left)) return false;
            if (!TryResolveOperand(items[1], facts, out var right)) return false;

            return op switch
            {
                "gte" => left >= right,
                "lte" => left <= right,
                "gt" => left > right,
                "lt" => left < right,
                _ => false,
            };
        }

        private static bool TryResolveOperand(JsonElement el, Dictionary<string, double> facts, out double value)
        {
            if (el.ValueKind == JsonValueKind.Number) { value = el.GetDouble(); return true; }
            if (el.ValueKind == JsonValueKind.String) return facts.TryGetValue(el.GetString() ?? "", out value);
            value = 0;
            return false;
        }
    }

    // Ресурс класса — общее имя для Focus Points/Ki/Rage и всего, что не HP/заклинания:
    // разные классы называют это по-разному, единая пара "текущее/максимум" покрывает все случаи
    // без отдельной сущности на каждый вид ресурса.
    public sealed record Pf2eResource(string Name, int Current, int Max);

    // Слоты заклинаний по уровню (0 = заговоры/cantrips). Известное/подготовленное заклинание —
    // отдельный список, не привязанный жёстко к слотам: и подготавливающие, и спонтанные касты
    // PF2e используют одну и ту же пару полей, разница только в том, меняется ли список между
    // отдыхами — это не моделируем, отслеживание "что подготовлено сегодня" на совести игрока.
    public sealed record Pf2eSpellSlotLevel(int Max, int Used);
    public sealed record Pf2eKnownSpell(string Name, int Level, bool Prepared);

    public sealed record Pf2eStatsModel
    {
        public string KeyAbility { get; set; } = "str";
        public int PerceptionRank { get; set; }
        public int ClassDcRank { get; set; }
        public Dictionary<string, int> SaveRanks { get; set; } = new() { ["fortitude"] = 0, ["reflex"] = 0, ["will"] = 0 };
        public Dictionary<string, int> SkillRanks { get; set; } = [];
        public string? SpellcastingTradition { get; set; }
        public int SpellcastingRank { get; set; }
        public List<Pf2eInventoryItem> Inventory { get; set; } = [];
        public List<Pf2eAttack> Attacks { get; set; } = [];
        public int HeroPoints { get; set; } = 1;
        public List<Pf2eResource> Resources { get; set; } = [];
        public List<Pf2eFeat> Feats { get; set; } = [];
        public Dictionary<int, Pf2eSpellSlotLevel> SpellSlots { get; set; } = [];
        public List<Pf2eKnownSpell> KnownSpells { get; set; } = [];

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public static Pf2eStatsModel FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new Pf2eStatsModel();
            try { return JsonSerializer.Deserialize<Pf2eStatsModel>(json, JsonOptions) ?? new Pf2eStatsModel(); }
            catch { return new Pf2eStatsModel(); }
        }

        public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);
    }
}
