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

    // R.1 — единственное per-класс-независимое правило прогрессии владений в PF2e (Core Rulebook
    // стр. 22): нельзя получить Мастера раньше 7-го уровня и Легенду раньше 15-го, вне зависимости
    // от класса. Точные уровни, на которых КАЖДЫЙ конкретный навык/спасбросок/Class DC реально
    // достигает этих рангов, отличаются по классу и не структурированы в данных — это остаётся
    // ручным выбором игрока на Pf2eStatsSheet, здесь только верхняя граница как подсказка/защита
    // от явной ошибки (можно поставить руками нетренированный уровнем раньше срока — это не гейт).
    public static int MinLevelForRank(int rank) => rank switch { 8 => 15, 6 => 7, _ => 1 };

    // Untrained в PF2e не получает бонус уровня — только Trained и выше.
    // N.6 — Proficiency Without Level (вариативное правило, тоггл на GameSession.
    // ProficiencyWithoutLevel): бонус владения не включает уровень персонажа вообще, только ранг.
    public static int Bonus(int rank, int level, bool proficiencyWithoutLevel = false) =>
        rank == 0 ? 0 : proficiencyWithoutLevel ? rank : rank + level;

    // L.1 — MAP: strikeIndex 0 = первая атака в ходу, 1 = вторая, 2+ = третья и далее.
    public static int MapPenalty(int strikeIndex, bool agile = false) => strikeIndex switch
    {
        <= 0 => 0,
        1 => agile ? -4 : -5,
        _ => agile ? -8 : -10
    };

    // L.2 — бонус атаки заклинанием и DC (классовая формула PF2e: 10 + владение + мод. хар-ки).
    public static int SpellAttackBonus(int spellcastingRank, int level, int abilityMod, bool proficiencyWithoutLevel = false) =>
        Bonus(spellcastingRank, level, proficiencyWithoutLevel) + abilityMod;

    public static int SpellDc(int spellcastingRank, int level, int abilityMod, bool proficiencyWithoutLevel = false) =>
        10 + Bonus(spellcastingRank, level, proficiencyWithoutLevel) + abilityMod;

    // N.6 — Automatic Bonus Progression (вариативное правило): персонаж получает числовые
    // бонусы к атаке/КЗ/спасброскам/Внимательности прямо от уровня, магическое оружие/доспехи
    // не нужны ради этих бонусов. Официальная таблица Core Rulebook: Attack Potency +1/+2/+3
    // на 2/10/16 уровне, Defense Potency +1/+2/+3 на 5/11/18, Saving Throw Potency +1/+2/+3
    // на 8/14/20, Perception Potency +1/+2/+3 на 7/13/19.
    private static readonly (int Level, int Bonus)[] AbpAttackTable = [(2, 1), (10, 2), (16, 3)];
    private static readonly (int Level, int Bonus)[] AbpDefenseTable = [(5, 1), (11, 2), (18, 3)];
    private static readonly (int Level, int Bonus)[] AbpSaveTable = [(8, 1), (14, 2), (20, 3)];
    private static readonly (int Level, int Bonus)[] AbpPerceptionTable = [(7, 1), (13, 2), (19, 3)];

    public enum AbpPotency { Attack, Defense, Save, Perception }

    public static int AbpBonus(AbpPotency potency, int level)
    {
        var table = potency switch
        {
            AbpPotency.Attack => AbpAttackTable,
            AbpPotency.Defense => AbpDefenseTable,
            AbpPotency.Save => AbpSaveTable,
            AbpPotency.Perception => AbpPerceptionTable,
            _ => AbpAttackTable,
        };
        var bonus = 0;
        foreach (var (lvl, b) in table)
            if (level >= lvl) bonus = b;
        return bonus;
    }

    // N.6 — формула КЗ персонажа: 10 + мод. Ловкости (ограничен Dex Cap надетой брони, если
    // есть) + бонус владения категорией брони (по рангу в ArmorProficiencyRanks, "unarmored"
    // без брони) + предметный бонус доспеха/щита + опционально Defense Potency (ABP). Раньше
    // КЗ существовало только как введённое вручную число на жетоне/листе персонажа — эта формула
    // не заменяет его автоматически (жетон остаётся источником истины в бою), а даёт
    // расчётное значение для отображения/сверки на листе персонажа.
    public static int ComputeArmorClass(
        int dexMod, Dictionary<string, int> armorProficiencyRanks, int level,
        bool proficiencyWithoutLevel, EquippedItemContext? armor, bool automaticBonusProgression)
    {
        var category = armor?.ArmorCategory ?? "unarmored";
        var rank = armorProficiencyRanks.GetValueOrDefault(category);
        var dexBonus = armor?.DexCap is { } cap ? Math.Min(dexMod, cap) : dexMod;
        var itemBonus = armor?.AcBonus ?? 0;
        var abp = automaticBonusProgression ? AbpBonus(AbpPotency.Defense, level) : 0;
        return 10 + dexBonus + Bonus(rank, level, proficiencyWithoutLevel) + itemBonus + abp;
    }

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
    public sealed record Pf2eAttack(
        string Name, int Rank, string AbilityKey, string DamageDice, int DamageBonus, string? DamageType,
        int? RangeFeet = null, int? ReachFeet = null);

    // Атака монстра (Pf2eMonster.AttacksJson, см. I.2) — в отличие от Pf2eAttack персонажей,
    // бонус атаки здесь уже готовое число из статблока, не считается из ранга+уровня+характеристики.
    public sealed record Pf2eMonsterAttack(
        string Name, int Bonus, string DamageDice, int DamageBonus, string? DamageType,
        int? RangeFeet = null, int? ReachFeet = null);

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

    // N.4 — иммунитет монстра: {type, exceptions[]}, без числовой величины (в отличие от
    // Pf2eDamageAdjustment) — иммунитет либо блокирует полностью, либо не применяется вовсе.
    // Type может быть как типом урона ("fire"), так и слагом состояния ("frightened").
    public sealed record Pf2eImmunity(string Type, List<string> Exceptions);

    public static List<Pf2eImmunity> ParseImmunities(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<Pf2eImmunity>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? []; }
        catch { return []; }
    }

    // N.5 — грубый парсер Pf2eSpell.Range (свободный текст из импортированных данных, формат
    // не нормализован: "30 feet", "60 feet (see text)", "1,000 feet", "1 mile", "touch", "self",
    // "unlimited", "varies", "planetary", "emanation up to 40-feet" и т.д.) в дальность в футах
    // для предупреждения при выборе цели вне дальности. Возвращает null, если дальность не
    // ограничена конкретным числом (self/unlimited/varies/planetary) — тогда проверка дистанции
    // просто пропускается, а не считается нарушением.
    private static readonly System.Text.RegularExpressions.Regex RangeNumberRegex = new(@"\d+");

    public static int? ParseRangeFeet(string? rangeText)
    {
        if (string.IsNullOrWhiteSpace(rangeText)) return null;
        var text = rangeText.ToLowerInvariant().Replace(",", "");

        if (text.Contains("self") || text.Contains("unlimited") || text.Contains("varies") || text.Contains("planetary"))
            return null;

        if (text.Contains("touch"))
            return 5;

        var match = RangeNumberRegex.Match(text);
        if (!match.Success) return null;

        var number = int.Parse(match.Value);
        var feet = text.Contains("mile") ? number * 5280 : number;

        // "0 feet"/мелейные заклинания — трактуем как дальность ближнего боя (радиус клетки),
        // иначе любая цель на соседней клетке ложно считалась бы "вне дальности".
        return feet == 0 ? 5 : feet;
    }

    // Q.3 — дальность/reach оружейной атаки: ranged/thrown → RangeFeet, melee → ReachFeet (reach trait = 10,
    // иначе 5 по умолчанию). Для предупреждения на столе (тот же паттерн, что N.5 для заклинаний).
    public static int AttackMaxRangeFeet(int? rangeFeet, int? reachFeet)
    {
        if (rangeFeet is > 0) return rangeFeet.Value;
        if (reachFeet is > 0) return reachFeet.Value;
        return 5;
    }

    public static (int? RangeFeet, int? ReachFeet) ParseWeaponRangeFromTraits(
        IReadOnlyList<string> traits, int? equipmentRange = null)
    {
        int? rangeFeet = equipmentRange is > 0 ? equipmentRange : null;
        int? reachFeet = null;
        foreach (var trait in traits)
        {
            if (trait == "reach")
                reachFeet = 10;
            if (trait.StartsWith("thrown-", StringComparison.Ordinal) &&
                int.TryParse(trait["thrown-".Length..], out var thrown))
                rangeFeet = thrown;
        }

        return (rangeFeet, reachFeet);
    }

    public static (int? RangeFeet, int? ReachFeet) ParseWeaponRangeFromStatsJson(string? statsJson)
    {
        if (string.IsNullOrWhiteSpace(statsJson)) return (null, null);
        try
        {
            using var doc = JsonDocument.Parse(statsJson);
            var root = doc.RootElement;
            var traits = ParseEquipmentTraits(root);
            int? equipmentRange = null;
            if (root.TryGetProperty("extra", out var extra) &&
                extra.TryGetProperty("range", out var range) &&
                range.ValueKind == JsonValueKind.Number)
                equipmentRange = range.GetInt32();
            return ParseWeaponRangeFromTraits(traits, equipmentRange);
        }
        catch { return (null, null); }
    }

    public static (int? RangeFeet, int? ReachFeet) ParseWeaponRangeFromTraitsString(string? traits, int? equipmentRange = null)
    {
        if (string.IsNullOrWhiteSpace(traits)) return ParseWeaponRangeFromTraits([], equipmentRange);
        var list = traits.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.ToLowerInvariant()).ToList();
        return ParseWeaponRangeFromTraits(list, equipmentRange);
    }

    // N.7 — аура монстра: радиус в футах + эффект-состояние. EffectSlug — тот же слаг состояния,
    // что и у ApplyTokenCondition (N.4/L.2), Value — опциональная величина (frightened 1 и т.п.).
    // R.1 — ауры со спасброском (Frightful Presence и аналоги): SaveDc не null означает, что
    // EffectSlug/Value накладываются только при ПРОВАЛЕ спасброска SaveType (fortitude/reflex/
    // will), а не автоматически всем в радиусе — раньше такие ауры (примерно половина всех аур
    // в данных) ошибочно извлекались как безусловные, см. Q.2 и правку в
    // build-pf2e-monster-automation.py. CriticalFailure* — отдельный, обычно более тяжёлый эффект
    // при критическом провале (типичная формулировка PF2e "Frightened 1 (Frightened 2 on a
    // critical failure)"); null, если критический провал даёт тот же эффект или не описан отдельно.
    // SaveDc == null — старое поведение без изменений: EffectSlug/Value накладывается всем в
    // радиусе без броска (безусловная аура).
    public sealed record Pf2eAura(
        int RadiusFeet, string EffectSlug, string EffectName, int? Value,
        string? SaveType = null, int? SaveDc = null,
        string? CriticalFailureSlug = null, string? CriticalFailureName = null, int? CriticalFailureValue = null);

    public static List<Pf2eAura> ParseAuras(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<Pf2eAura>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? []; }
        catch { return []; }
    }

    // Дистанция между центрами двух жетонов в футах — тот же расчёт, что у AOE-шаблонов (J.5,
    // Table.razor.cs RecomputeTemplateAffectedTokens): плоская евклидова дистанция, 1 клетка = 5 фт.
    public static double TokenDistanceFeet(double ax, double ay, double aw, double ah, double bx, double by, double bw, double bh)
    {
        var dx = (bx + bw / 2.0 - ax - aw / 2.0) * 5;
        var dy = (by + bh / 2.0 - ay - ah / 2.0) * 5;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    // Структурированный фит: имя, уровень, и опционально Slug — связь с записью в общем
    // справочнике RuleEntry (Feat). Slug заполняется, только если фит выбран из автодополнения
    // по каталогу (см. Detail.razor "datalist"); свободный текст без совпадения по каталогу
    // остаётся с Slug = null и не участвует в автоматизации правил (J.1) — просто текст, как раньше.
    // N.10 — SelectedChoice: значение, выбранное игроком для ChoiceSet фита (см. Pf2eFeatChoiceSet
    // ниже), добавляется в roll options как "{selector}:{значение}". ToggledOptions — включённые
    // RollOption-тумблеры фита (см. Pf2eFeatRollOption), каждый добавляется в roll options как есть.
    // N.6 — Source: из какого слота игрок взял ЭТОТ конкретный фит на листе (не путать с
    // "category" в справочнике RuleEntry — той же классификацией у самого фита как записи
    // каталога вообще, из какого он источника по правилам). Source — выбор игрока, не
    // свойство фита: один и тот же классовый фит теоретически можно взять и в обычный
    // классовый слот, и (при гомебрю) в бонусный. Значения: "class"/"ancestry"/"skill"/
    // "general"/"archetype"/"bonus"; null — не размечено (старые записи, обратная совместимость).
    // Нужен для Free Archetype (считать, сколько фитов взято именно из архетипных слотов) и
    // закрывает пробел из N.10 про "система не ведёт учёт, откуда взялся фит".
    public sealed record Pf2eFeat(string Name, int Level, string? Slug = null,
        string? SelectedChoice = null, List<string>? ToggledOptions = null, string? Source = null);

    public static readonly (string Key, string Label)[] FeatSources =
        [("class", "Классовый слот"), ("ancestry", "Предковый слот"), ("skill", "Навыковый слот"),
         ("general", "Общий слот"), ("archetype", "Архетипный слот (Free Archetype)"), ("bonus", "Бонусный")];

    // N.6 — Free Archetype: один архетипный фит на каждый чётный уровень (2,4,6...20 = 10 фитов
    // максимум). Только ориентир для сверки на листе/столе — не блокирует добавление фитов сверх
    // нормы (GM решает сам, как и с прочими вручную вводимыми числами в этой системе).
    public static int ExpectedFreeArchetypeFeats(int level) => level / 2;

    // N.6 — Gradual Ability Boosts: под этим правилом повышение полагается на каждом уровне
    // (не только на 5/10/15/20, как обычно) — возвращает уровни от 1 до текущего, за которые
    // повышение ещё не отмечено (loggedLevels), чтобы напомнить игроку/ГМу.
    public static List<int> PendingGradualAbilityBoosts(int level, IReadOnlyCollection<int> loggedLevels) =>
        Enumerable.Range(1, Math.Max(level, 0)).Where(l => !loggedLevels.Contains(l)).ToList();

    // R.1 — Билдер/прогрессия: стандартные (не Gradual) уровни повышения характеристик —
    // 4 буста на выбор игрока каждый раз, PF2e Core Rulebook стр. 22. AbilityBoostLevels
    // (см. комментарий выше) используется тем же полем и под этим режимом — "уровень отмечен"
    // означает "4 буста этого уровня применены", а не "по одному разу за каждый уровень".
    public static readonly int[] StandardAbilityBoostLevels = [5, 10, 15, 20];

    public static readonly (string Code, string Label)[] AbilityCodes =
        [("str", "Сила"), ("dex", "Ловкость"), ("con", "Телосложение"),
         ("int", "Интеллект"), ("wis", "Мудрость"), ("cha", "Харизма")];

    // PF2e: буст поднимает характеристику на +2, но только на +1, если она уже 18 или выше —
    // не даём "разгонять" одну характеристику бесконечно быстрее остальных.
    public static int ApplyAbilityBoost(int score) => score >= 18 ? score + 1 : score + 2;

    // Стандартные уровни повышения характеристик, ещё не отмеченные в AbilityBoostLevels, вплоть
    // до newLevel включительно — считает и пропущенные при скачке на несколько уровней разом.
    public static List<int> DueStandardAbilityBoostLevels(int newLevel, IReadOnlyCollection<int> loggedLevels) =>
        StandardAbilityBoostLevels.Where(l => l <= newLevel && !loggedLevels.Contains(l)).ToList();

    // R.1 — Слоты фитов по уровню: единая для всех классов PF2e таблица (Core Rulebook стр. 32) —
    // в отличие от прогрессии владений/HP, номера уровней слотов фитов не зависят от класса.
    // Повышение навыка (skill increase) сюда же добавлено для полноты чек-листа "что нового",
    // хотя это не фит-слот — авто-применения нет, игрок сам поднимает ранг на листе (Pf2eStatsSheet).
    public static readonly (string Key, string Label, int[] Levels)[] FeatSlotTable =
    [
        ("ancestry", "Предковый фит", [1, 5, 9, 13, 17]),
        ("skill", "Навыковый фит", [2, 4, 6, 8, 10, 12, 14, 16, 18, 20]),
        ("general", "Общий фит", [3, 7, 11, 15, 19]),
        ("class", "Классовый фит", [2, 4, 6, 8, 10, 12, 14, 16, 18, 20]),
        ("skill-increase", "Повышение навыка", [3, 5, 7, 9, 11, 13, 15, 17, 19]),
    ];

    // Что нового открылось между старым и новым уровнем — чисто информационный чек-лист для
    // экрана level-up, не блокирует и не применяет ничего автоматически (те же принципы, что
    // у ExpectedFreeArchetypeFeats/PendingGradualAbilityBoosts — подсказка, не гейт).
    public static List<(int Level, string Label)> NewFeatSlotsBetweenLevels(int oldLevel, int newLevel)
    {
        var result = new List<(int, string)>();
        foreach (var (_, label, levels) in FeatSlotTable)
            foreach (var lvl in levels)
                if (lvl > oldLevel && lvl <= newLevel)
                    result.Add((lvl, label));
        return [.. result.OrderBy(x => x.Item1)];
    }

    // Числовой модификатор из данных фита (J.1) — селектор: "land-speed", ключ навыка,
    // "perception" или "ac". Predicate (может быть null = безусловный) — сырое PF2e-условие
    // из Foundry: массив строк (все должны быть истинны) и/или объектов {or,not,nor,nand,gte,
    // lte,gt,lt} — вычисляется PredicateEvaluator против статичных фактов персонажа.
    public sealed record Pf2eFlatModifier(string Selector, int Value, string Type, JsonElement? Predicate);

    // N.10 — Foundry ChoiceSet: фит предлагает выбор одного варианта (стихия, оружие, навык...),
    // выбор персистится в Pf2eFeat.SelectedChoice и подставляется в roll options как
    // "{Selector}:{значение варианта}", от которого дальше зависят предикаты модификаторов
    // (в т.ч. модификаторов того же фита — например "ranger-hunters-edge:flurry" включает
    // модификатор multiple-attack-penalty только при выборе Flurry).
    public sealed record Pf2eFeatChoiceOption(string Value, string Label);
    public sealed record Pf2eFeatChoiceSet(string Selector, string Prompt, List<Pf2eFeatChoiceOption> Options);

    // N.10 — Foundry RollOption toggle: тумблер прямо на листе персонажа ("Раскрыта тайная
    // техника: вкл/выкл"), включённое состояние персистится в Pf2eFeat.ToggledOptions и
    // добавляется в roll options как есть — от него зависят предикаты модификаторов.
    public sealed record Pf2eFeatRollOption(string Option, string Label, bool Default);

    public sealed record Pf2eFeatStats(
        List<Pf2eFlatModifier> Modifiers, List<string>? Grants,
        Pf2eFeatChoiceSet? ChoiceSet = null, List<Pf2eFeatRollOption>? RollOptions = null);

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

    public static Pf2eFeatChoiceSet? ParseFeatChoiceSet(string? statsJson)
    {
        if (string.IsNullOrWhiteSpace(statsJson)) return null;
        try
        {
            var stats = JsonSerializer.Deserialize<Pf2eFeatStats>(statsJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return stats?.ChoiceSet;
        }
        catch { return null; }
    }

    public static List<Pf2eFeatRollOption> ParseFeatRollOptions(string? statsJson)
    {
        if (string.IsNullOrWhiteSpace(statsJson)) return [];
        try
        {
            var stats = JsonSerializer.Deserialize<Pf2eFeatStats>(statsJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return stats?.RollOptions ?? [];
        }
        catch { return []; }
    }

    // N.10 — собирает roll options из выбора игрока (ChoiceSet) и включённых тумблеров
    // (RollOption) по всем фитам персонажа — общий helper для листа персонажа и стола (Table.razor).
    public static void AddFeatChoiceRollOptions(
        HashSet<string> options, IEnumerable<Pf2eFeat> feats, IReadOnlyDictionary<string, string> statsJsonBySlug)
    {
        foreach (var feat in feats)
        {
            if (feat.Slug is null || !statsJsonBySlug.TryGetValue(feat.Slug, out var statsJson))
                continue;

            if (feat.SelectedChoice is { Length: > 0 } choice)
            {
                var choiceSet = ParseFeatChoiceSet(statsJson);
                if (choiceSet is not null)
                    options.Add($"{choiceSet.Selector}:{choice}");
            }

            if (feat.ToggledOptions is { Count: > 0 } toggled)
                foreach (var option in toggled)
                    options.Add(option);
        }
    }

    // L.4 — контекст экипировки для roll options item:* / armor:* и модификаторов из stats_json.
    // N.6 — DexCap/AcBonus добавлены для формулы КЗ (ComputeArmorClass): ограничение бонуса
    // Ловкости и сам предметный бонус доспеха/щита, взятые из тех же extra.dex_cap/extra.ac_bonus,
    // что уже парсит ParseItemModifiers для отдельного модификатора — здесь нужны сырыми для формулы.
    public sealed record EquippedItemContext(
        string Slug, string? ItemKind, string? ArmorCategory,
        IReadOnlyList<string> Traits, bool IsRanged, string? DamageCategory,
        int? DexCap = null, int AcBonus = 0);

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
            int? dexCap = null;
            var acBonus = 0;
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
                if (extra.TryGetProperty("dex_cap", out var dc) && dc.TryGetInt32(out var dcVal))
                    dexCap = dcVal;
                if (extra.TryGetProperty("ac_bonus", out var ab) && ab.TryGetInt32(out var abVal))
                    acBonus = abVal;
            }

            var traits = ParseEquipmentTraits(root);
            if (traits.Contains("ranged") || traits.Contains("thrown")) isRanged = true;

            return new EquippedItemContext(slug, kind, armorCat, traits, isRanged, damageCategory, dexCap, acBonus);
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
    // IsFocus — заклинание берётся из общего пула Focus Points, а не из слотов по уровням
    // (см. Pf2eStatsModel.FocusPoints). Default false сохраняет обратную совместимость с уже
    // сохранённым Pf2eStatsJson без этого поля — System.Text.Json подставит false при десериализации.
    public sealed record Pf2eKnownSpell(string Name, int Level, bool Prepared, bool IsFocus = false);

    // N.2 — известная формула создания предмета (Formula Book). Slug — связь со справочником
    // (RuleCategory.Equipment), заполняется по тому же принципу, что и у Pf2eFeat: подтягивается
    // при точном совпадении имени, свободный текст без совпадения остаётся Slug = null.
    public sealed record Pf2eKnownFormula(string Name, int Level, string? Slug = null);

    // Стандартная таблица DC по уровню (Core Rulebook table 10-5) — используется и для Craft,
    // и в принципе для любой "проверки против уровня предмета/угрозы", а не только крафта.
    private static readonly int[] StandardDcByLevel =
    [
        14, 15, 16, 18, 19, 20, 22, 23, 24, 26, 27, 28, 30, 31, 32, 34, 35, 36, 38, 39, 40, 42, 44, 46, 48, 50
    ];

    public static int StandardDc(int level) =>
        StandardDcByLevel[Math.Clamp(level, 0, StandardDcByLevel.Length - 1)];

    public enum DegreeOfSuccess { CriticalFailure, Failure, Success, CriticalSuccess }

    // Общее правило степеней успеха PF2e: натуральная 20/1 сдвигает степень на одну ступень
    // в соответствующую сторону, превышение/провал DC на 10+ — критический результат.
    public static DegreeOfSuccess RollDegree(int naturalRoll, int total, int dc)
    {
        var degree = total >= dc + 10 ? DegreeOfSuccess.CriticalSuccess
            : total >= dc ? DegreeOfSuccess.Success
            : total <= dc - 10 ? DegreeOfSuccess.CriticalFailure
            : DegreeOfSuccess.Failure;

        if (naturalRoll == 20 && degree < DegreeOfSuccess.CriticalSuccess) degree++;
        if (naturalRoll == 1 && degree > DegreeOfSuccess.CriticalFailure) degree--;
        return degree;
    }

    // N.11 — степень успеха уже вшита текстом в сообщение чата (RollDiceCommandHandler:
    // "... vs DC {dc} → {DegreeLabel}"), клиент не дублирует бросок/сравнение — просто
    // распознаёт готовый результат тем же способом, что и подсветка строки (DegreeStyle
    // в Table.razor.cs), чтобы повесить на сообщение кнопку "Применить".
    private static readonly System.Text.RegularExpressions.Regex CheckResultRegex =
        new(@"vs DC (\d+) → (.+)$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public static bool TryParseCheckResult(string content, out int dc, out DegreeOfSuccess degree, out bool isAttack)
    {
        dc = 0;
        degree = DegreeOfSuccess.Failure;
        isAttack = false;

        var match = CheckResultRegex.Match(content);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out dc))
            return false;

        var label = match.Groups[2].Value.Trim();
        switch (label)
        {
            case "Критический успех!": degree = DegreeOfSuccess.CriticalSuccess; break;
            case "Успех": degree = DegreeOfSuccess.Success; break;
            case "Провал": degree = DegreeOfSuccess.Failure; break;
            case "Критический провал!": degree = DegreeOfSuccess.CriticalFailure; break;
            default: return false;
        }

        isAttack = content.Contains("(атака", StringComparison.OrdinalIgnoreCase);
        return true;
    }

    // Стандартное правило PF2e применения урона по степени успеха: атака (сравнение с КЗ) —
    // крит удваивает, провал/крит.провал = промах (0); спасбросок против эффекта (сравнение
    // с DC) — крит.успех без эффекта (0), успех = половина урона, провал = полный, крит.провал
    // = удвоенный.
    public static double DamageMultiplierForDegree(DegreeOfSuccess degree, bool isAttack) =>
        isAttack
            ? degree switch
            {
                DegreeOfSuccess.CriticalSuccess => 2.0,
                DegreeOfSuccess.Success => 1.0,
                _ => 0.0,
            }
            : degree switch
            {
                DegreeOfSuccess.CriticalSuccess => 0.0,
                DegreeOfSuccess.Success => 0.5,
                DegreeOfSuccess.Failure => 1.0,
                DegreeOfSuccess.CriticalFailure => 2.0,
                _ => 1.0,
            };

    // N.12 — таблица случайных встреч ГМа: одна таблица на сессию, роняется в
    // GameSession.EncounterTableJson как есть, сервер разбирает тот же формат только для
    // самого броска (RollEncounterTableCommandHandler). MonsterId — тот же Guid, что и
    // Pf2eMonster.Id, чтобы кнопка "Добавить жетон" переиспользовала обычный AddTableTokenAsync.
    public sealed record Pf2eEncounterEntry(int Min, int Max, string Label, Guid? MonsterId);
    public sealed record Pf2eEncounterTable(string Title, List<Pf2eEncounterEntry> Entries);

    public static Pf2eEncounterTable? ParseEncounterTable(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<Pf2eEncounterTable>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)); }
        catch { return null; }
    }

    public static string SerializeEncounterTable(Pf2eEncounterTable table) =>
        JsonSerializer.Serialize(table, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    // Результат броска таблицы встреч приходит текстом в чат (RollEncounterTableCommandHandler:
    // "... → {label} [[monster:{id}]]") — тот же приём, что и у N.11 (структурные данные
    // зашиты в готовый текст, клиент их достаёт регэкспом, не пересчитывая бросок заново).
    private static readonly System.Text.RegularExpressions.Regex EncounterMonsterMarkerRegex =
        new(@"\[\[monster:([0-9a-fA-F-]{36})\]\]", System.Text.RegularExpressions.RegexOptions.Compiled);

    public static Guid? TryParseEncounterMonsterMarker(string content)
    {
        var match = EncounterMonsterMarkerRegex.Match(content);
        return match.Success && Guid.TryParse(match.Groups[1].Value, out var id) ? id : null;
    }

    public static string StripEncounterMonsterMarker(string content) =>
        EncounterMonsterMarkerRegex.Replace(content, "").TrimEnd();

    // N.12 — генератор NPC: чисто клиентская случайная комбинация имени и черты/причуды, без
    // сохранения — разовая подсказка ГМу для безымянного персонажа на лету, не полноценный
    // "лист персонажа" (для этого есть Character/Pf2eStatsSheet).
    private static readonly string[] NpcFirstNamesMale =
        ["Гаррет", "Донован", "Кассий", "Эдрик", "Финнеган", "Ивар", "Магнус", "Освальд", "Родерик", "Тобиас", "Уилфред", "Ярек"];
    private static readonly string[] NpcFirstNamesFemale =
        ["Аделина", "Брианна", "Кассандра", "Далия", "Эления", "Фиона", "Изольда", "Мирабель", "Ровена", "Сильвия", "Тесса", "Ярина"];
    private static readonly string[] NpcSurnames =
        ["Чернолесье", "Крутогор", "Быстрый Клинок", "Тихий Шаг", "Огнегрив", "Дубощит", "Серебряная Река", "Полынь", "Ветродуй", "Каменный Кулак"];
    private static readonly string[] NpcTraits =
        [
            "постоянно грызёт кончик пера, даже когда не пишет",
            "никогда не смотрит собеседнику в глаза дольше пары секунд",
            "коллекционирует пуговицы от чужой одежды «на память»",
            "говорит о себе в третьем лице, когда нервничает",
            "держит при себе засушенный цветок и не объясняет почему",
            "разговаривает с животными как с равными",
            "запоминает любую услышанную мелодию с одного раза",
            "боится закрытых дверей — всегда оставляет щель",
            "торгуется даже там, где торг неуместен",
            "точит нож каждый раз, когда думает",
            "носит слишком много колец не по размеру",
            "цитирует старинные пословицы, часто невпопад",
        ];

    public sealed record Pf2eGeneratedNpc(string Name, string Trait);

    public static Pf2eGeneratedNpc GenerateNpc(Random? random = null)
    {
        random ??= Random.Shared;
        var isFemale = random.Next(2) == 0;
        var first = (isFemale ? NpcFirstNamesFemale : NpcFirstNamesMale)[random.Next(isFemale ? NpcFirstNamesFemale.Length : NpcFirstNamesMale.Length)];
        var surname = NpcSurnames[random.Next(NpcSurnames.Length)];
        var trait = NpcTraits[random.Next(NpcTraits.Length)];
        return new Pf2eGeneratedNpc($"{first} {surname}", trait);
    }

    // N.6 — Критические/провальные колоды (вариативное правило): вместо "просто x2 урона"/
    // "просто провал" GM тянет случайную карту с дополнительным эффектом. Официальные колоды
    // Paizo ("Critical Hit Deck"/"Critical Fumble Deck") — платный физический продукт, поэтому
    // здесь курируемый набор эффектов в духе официальных карт (не дословный текст), тот же
    // принцип "разовая подсказка ГМу, без сохранения", что и GenerateNpc.
    private static readonly string[] CritCards =
        [
            "Отбросить — цель отброшена на 10 футов от вас и падает (Лежащий), если врезается в преграду.",
            "Оглушить — цель Ошеломлена 1.",
            "Кровотечение — цель получает состояние Кровотечение с уроном, равным половине урона от этого удара.",
            "Ослепить — цель Ослеплена до конца своего следующего хода.",
            "Обезоружить — если у цели есть оружие в руках, оно вылетает и падает в 10 футах.",
            "Двойной укол — нанесите дополнительно урон, равный вашей характеристике силы/ловкости к этому удару.",
            "Смертельная рана — цель получает состояние Ослабленный 1 до конца схватки.",
            "Пробить защиту — щит цели (если есть) теряет прочность, равную нанесённому урону.",
            "Оступиться — цель Лежащая и должна потратить действие, чтобы встать.",
            "Идеальное попадание — эффект не срабатывает, просто эффектное описание удара по вкусу GM.",
        ];

    private static readonly string[] FumbleCards =
        [
            "Потеря равновесия — вы становитесь Лежащим.",
            "Уронили оружие — ваше оружие падает у ваших ног.",
            "Задели союзника — ближайший союзник в пределах досягаемости получает половину вашего урона от промаха.",
            "Открылись — вы Ошеломлены 1 на свой следующий ход.",
            "Испортили позицию — вы отступаете на 5 футов не по своей воле (если есть куда).",
            "Сломали оружие — ваше оружие получает состояние Повреждено (пока не почините).",
            "Потратили время впустую — ваше действие потрачено без эффекта, ничего худшего не происходит.",
            "Растерялись — следующая ваша проверка до конца хода получает штраф −2.",
        ];

    public static string DrawCritCard(Random? random = null) => CritCards[(random ?? Random.Shared).Next(CritCards.Length)];
    public static string DrawFumbleCard(Random? random = null) => FumbleCards[(random ?? Random.Shared).Next(FumbleCards.Length)];

    public sealed record Pf2eStatsModel
    {
        public string KeyAbility { get; set; } = "str";
        public int PerceptionRank { get; set; }
        public int ClassDcRank { get; set; }
        // N.6 — владение категориями брони для формулы КЗ (ComputeArmorClass): ключи
        // "unarmored"/"light"/"medium"/"heavy", те же значения ранга, что у SkillRanks
        // (0/2/4/6/8 = untrained/trained/expert/master/legendary).
        public Dictionary<string, int> ArmorProficiencyRanks { get; set; } = new() { ["unarmored"] = 0, ["light"] = 0, ["medium"] = 0, ["heavy"] = 0 };
        // N.6 — Gradual Ability Boosts: сами значения характеристик (Str/Dex/...) редактируются
        // на Character/Detail.razor вне PF2e-листа, эта система их не пересчитывает — только
        // отмечает, за какие уровни повышение уже учтено (чтобы не забыть/не задвоить при +1
        // за каждый уровень вместо +2 разом на 5/10/15/20).
        public List<int> AbilityBoostLevels { get; set; } = [];
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
        // Фокус-поинты — отдельный пул, восстанавливается за 10-минутный отдых (не как обычные
        // слоты — те только на полном отдыхе), поэтому раньше жили в общем Resources без разбора.
        // Теперь отдельное поле — тот же паттерн Max/Used, что и у слотов заклинаний.
        public Pf2eSpellSlotLevel FocusPoints { get; set; } = new(0, 0);
        public List<Pf2eKnownSpell> KnownSpells { get; set; } = [];
        public List<Pf2eKnownFormula> KnownFormulas { get; set; } = [];

        // Полировка "экзотические предикаты" — PF2e-фиты используют сотни узких одноразовых
        // предикатных флагов, привязанных к конкретным способностям/ритуалам/боевым формам
        // (target:mark:*, spell-effect:*, battle-form:*, origin:*, disguise:* и десятки других
        // уникальных для одного фита — их невозможно разумно перечислить в коде по одному).
        // Вместо этого — ручной эскейп-хэтч: GM/игрок сам вписывает roll option строкой (то же
        // самое, что предикат фита ожидает увидеть), она добавляется в BuildCombatRollOptions/
        // RefreshFeatModifiersAsync наравне со всеми автоматическими — покрывает любой предикат,
        // включая те, что появятся в будущем контенте, без правки кода под каждый новый случай.
        public List<string> CustomRollOptions { get; set; } = [];

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
