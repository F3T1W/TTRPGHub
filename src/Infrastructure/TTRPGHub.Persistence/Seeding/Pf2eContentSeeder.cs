using System.Text.Json;
using Microsoft.Extensions.Logging;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Seeding;

// Расширяет справочник PF2e за пределы классов/предков/спеллов/монстров (см. Pf2eRulesSeeder,
// Pf2eImporter): универсальные действия, фиты, снаряжение, бэкграунды, состояния. Контент написан
// вручную сразу на русском — открытого API PF2e на русском нет. Это стартовый набор, не полный
// свод правил (см. ROADMAP.md за списком того, чего ещё не хватает).
public sealed class Pf2eContentSeeder(
    IGameSystemRepository systemRepository,
    IRuleEntryRepository entryRepository,
    IUnitOfWork unitOfWork,
    ILogger<Pf2eContentSeeder> logger)
{
    public async Task SeedIfEmptyAsync(CancellationToken ct = default)
    {
        var system = await systemRepository.GetBySlugAsync("pf2e", ct);
        if (system is null)
        {
            logger.LogWarning("Система pf2e ещё не создана — пропускаю посев расширенного контента");
            return;
        }

        await SeedCategoryAsync(system.Id, RuleCategory.Action, BuildActions, "универсальных действий", ct);
        await SeedCategoryAsync(system.Id, RuleCategory.Condition, BuildConditions, "состояний", ct);
        await SeedCategoryAsync(system.Id, RuleCategory.Feat, BuildFeats, "фитов", ct);
        await SeedCategoryAsync(system.Id, RuleCategory.Equipment, BuildEquipment, "предметов снаряжения", ct);
        await SeedCategoryAsync(system.Id, RuleCategory.Equipment, BuildRunes, "фундаментальных/свойственных рун", ct);
        await SeedCategoryAsync(system.Id, RuleCategory.Background, BuildBackgrounds, "бэкграундов", ct);
    }

    // Проверка по каждому slug (не AnyAsync(category) целиком) — та же идемпотентность, что и в
    // Pf2eRulesSeeder: расширение исходных данных (например, добавление рун в pf2e-equipment.json)
    // подхватывается на уже засеянной БД, а не требует ручного вайпа категории.
    private async Task SeedCategoryAsync(
        GameSystemId systemId, RuleCategory category, Func<GameSystemId, IEnumerable<RuleEntry>> build,
        string label, CancellationToken ct)
    {
        var entries = build(systemId).ToList();
        if (entries.Count == 0)
            return;

        var existingSlugs = (await entryRepository.GetBySlugsAsync(systemId, category, entries.Select(e => e.Slug).ToList(), ct))
            .Select(e => e.Slug).ToHashSet();
        var missing = entries.Where(e => !existingSlugs.Contains(e.Slug)).ToList();
        if (missing.Count == 0)
            return;

        await entryRepository.AddRangeAsync(missing, ct);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Добавлено {Count} новых {Label} PF2e", missing.Count, label);
    }

    // ── Универсальные действия ──────────────────────────────────────────────────

    private static IEnumerable<RuleEntry> BuildActions(GameSystemId systemId)
    {
        (string slug, string name, int actions, string traits, string desc)[] data =
        [
            ("strike", "Удар", 1, "атака",
                "Совершите одну атаку оружием или безоружную атаку. Каждая следующая атака в тот же ход получает штраф многократной атаки (MAP): −5 за вторую атаку, −4 если оружие «агитированное» (agile), −10 за третью и далее (−8 для agile)."),
            ("stride", "Движение", 1, "передвижение",
                "Переместитесь на расстояние своей Скорости по прямой доступному пути, избегая опасной местности, если явно не сказано иное."),
            ("step", "Шаг", 1, "передвижение",
                "Переместитесь на 5 футов, не провоцируя реакций типа «атака за уход» (attack of opportunity)."),
            ("raise-a-shield", "Поднять щит", 1, "-",
                "Если у вас в руках щит, вы поднимаете его, получая бонус щита к КЗ до начала вашего следующего хода."),
            ("recall-knowledge", "Вспомнить знания", 1, "концентрация",
                "Сделайте проверку соответствующего навыка (Общество, Природа, Религия, Аркана и т.д.), чтобы вспомнить факт о существе, предмете или ситуации."),
            ("seek", "Осмотреться", 1, "концентрация",
                "Внимательно осмотрите область вокруг себя, пытаясь заметить скрытые или невидимые существа/объекты в пределах 30 футов."),
            ("demoralize", "Запугать", 1, "аудиальное, эмоция, ментальное, концентрация",
                "Проверка Запугивания против Воли цели, которая понимает вас. При успехе цель получает состояние «Испуганный 1» (Frightened 1), при критическом успехе — «Испуганный 2»."),
            ("aid", "Помощь", 1, "-",
                "Готовите помочь союзнику в следующей проверке или атаке — если ваша собственная подготовительная проверка успешна, союзник получает бонус +1 (+2 при критическом успехе) к своей проверке."),
            ("escape", "Побег", 1, "атака",
                "Проверка Атлетики, Акробатики или иного применимого метода против Сложности захвата/удержания, чтобы освободиться от состояния «Схвачен» (Grabbed) или «Скован» (Restrained)."),
            ("grapple", "Захват", 1, "атака",
                "Проверка Атлетики против Рефлекса цели, чтобы наложить на неё состояние «Схвачен» при успехе, либо оба падаете в захвате при критическом успехе."),
            ("trip", "Подножка", 1, "атака",
                "Проверка Атлетики против Рефлекса цели (без тяжёлых предметов в руках), чтобы повалить её (состояние «Лежащий», Prone) при успехе."),
            ("disarm", "Разоружение", 1, "атака",
                "Проверка Атлетики против Рефлекса цели, чтобы выбить оружие или предмет у неё из рук."),
            ("shove", "Толчок", 1, "атака",
                "Проверка Атлетики против Рефлекса цели, чтобы оттолкнуть её на 5 футов (10 футов при критическом успехе)."),
            ("delay", "Задержка", 0, "-",
                "Откладываете свой ход на более поздний момент в этом же раунде инициативы, сохраняя оставшиеся действия."),
            ("ready", "Готовность", 2, "-",
                "Готовите одно действие с условием — когда условие срабатывает, вы немедленно совершаете это действие как реакцию."),
            ("sustain-a-spell", "Поддержание заклинания", 1, "концентрация",
                "Поддерживаете эффект заклинания с длительностью «поддерживается» ещё на один раунд — доступно только в раунде, следующем сразу после сотворения."),
            ("take-cover", "Укрыться", 1, "-",
                "Используете имеющееся укрытие более эффективно, увеличивая бонус от него к КЗ и спасброскам Рефлекса."),
            ("hustle", "Ускоренный марш", 0, "передвижение, исследование",
                "Во время передвижения по карте перемещаетесь с удвоенной скоростью, но не можете поддерживать этот темп долго без риска утомления."),
            ("point-out", "Указать цель", 1, "манипуляция, ментальное",
                "Указываете союзникам на существо, которое не обнаружено или скрыто для них, но обнаружено вами — для них оно становится скрытым вместо не обнаруженного."),
            ("request", "Просьба", 1, "аудиальное, ментальное",
                "Просите союзника выполнить одно из его заготовленных заранее действий по кодовому слову."),
            ("coerce", "Принуждение", 1, "аудиальное, эмоция, ментальное",
                "Проверка Запугивания против Воли цели, чтобы заставить её выполнить простую просьбу под угрозой."),
            ("perform", "Выступление", 1, "аудиальное, визуальное (зависит от типа)",
                "Проверка Выступления одним из выбранных способов (петь, играть, танцевать и т.д.), чтобы развлечь зрителей или заработать."),
            ("treat-wounds", "Лечение ран", 1, "манипуляция",
                "Проверка Медицины (нужен набор целителя), чтобы восстановить HP цели вне боя; после успеха или провала над той же целью нельзя повторить, пока не пройдёт время."),
            ("search", "Поиск", 1, "концентрация, секретное",
                "Активно исследуете область на наличие скрытых объектов или существ во время передвижения — аналог продлённого действия Осмотреться."),
            ("sense-motive", "Почувствовать намерение", 1, "концентрация, секретное",
                "Проверка Проницательности, чтобы заметить, что существо лжёт, притворяется или готовит что-то подозрительное."),
            ("high-jump", "Прыжок в высоту", 2, "передвижение",
                "Совершаете разбег (минимум 10 футов по прямой) и проверку Атлетики, чтобы перепрыгнуть препятствие по высоте."),
            ("long-jump", "Прыжок в длину", 2, "передвижение",
                "Совершаете разбег и проверку Атлетики, чтобы преодолеть расстояние прыжком — дальность зависит от степени успеха."),
            ("swim", "Плавание", 1, "передвижение",
                "Проверка Атлетики для передвижения в воде, если у существа нет скорости плавания."),
            ("climb", "Лазание", 1, "передвижение",
                "Проверка Атлетики для передвижения по вертикальной или ненадёжной поверхности, если у существа нет скорости лазания."),
            ("craft", "Изготовление", 0, "манипуляция, исследование",
                "Проверка Ремесла в рамках длительного изготовления предмета из формулы, которую персонаж знает."),
            ("repair", "Починка", 1, "манипуляция",
                "Проверка Ремесла, чтобы восстановить прочность повреждённого предмета или конструкции."),
        ];

        return data.Select(a => RuleEntry.Create(
            systemId, RuleCategory.Action, a.slug, a.name,
            summary: a.actions > 0 ? $"{a.actions} действ. · {a.traits}" : $"Реакция/свободное · {a.traits}",
            contentMarkdown: a.desc,
            statsJson: JsonSerializer.Serialize(new { action_cost = a.actions, traits = a.traits }),
            tags: ["действие", "PF2e"], isHomebrew: false, source: "PF2e SRD"));
    }

    // ── Состояния — реальные официальные данные Paizo (ORC/OGL), см. Seeding/Data/README.md ──

    private sealed record SeedConditionModifier(List<string> Selectors, string Type, Dictionary<string, object> Formula);
    private sealed record SeedCondition(
        string Slug, string Name, bool IsValued, string Description,
        List<SeedConditionModifier> Modifiers, string Source, string License);

    // Формулы вида "-@item.badge.value" (штраф = значение состояния) размечены как
    // { kind: "per_badge", multiplier: -1 } скриптом-экстрактором — этого достаточно для
    // будущего combat tracker (J.2), чтобы применять Frightened 2 как -2 автоматически. Пока нет
    // движка бросков — данные просто хранятся структурированно вместо текста, не применяются сами.
    private static IEnumerable<RuleEntry> BuildConditions(GameSystemId systemId)
    {
        var seeds = LoadEmbeddedJson<SeedCondition>("pf2e-conditions.json");
        return seeds.Select(c => RuleEntry.Create(
            systemId, RuleCategory.Condition, c.Slug, c.Name,
            summary: c.IsValued ? "Состояние со значением (стакается числом)" : "Состояние без значения (есть/нет)",
            contentMarkdown: c.Description,
            statsJson: JsonSerializer.Serialize(new
            {
                has_value = c.IsValued,
                modifiers = c.Modifiers.Select(m => new { selectors = m.Selectors, type = m.Type, formula = m.Formula }),
            }),
            tags: ["состояние", "PF2e"], isHomebrew: false,
            source: $"{c.Source} (Foundry pf2e system data, {c.License})"));
    }

    // ── Фиты — реальные официальные данные Paizo (ORC), см. Seeding/Data/README.md ──

    private sealed record SeedFeatModifier(string Selector, int Value, string Type, object? Predicate);

    private sealed record SeedFeat(
        string Slug, string Name, int Level, string Category, string ActionType, int? Actions,
        string Traits, string Prerequisites, string Description,
        List<SeedFeatModifier> Modifiers, List<string> Grants, string Source);

    // J.1 (автоматизация правил): FlatModifier-правила на скорость/навыки/AC/восприятие
    // перенесены в `modifiers` вместе с их `predicate` (условие вида "если...") как есть —
    // предикат вычисляется на клиенте (Pf2eLookups.PredicateEvaluator) против статичных фактов
    // персонажа (уровень, ранги навыков, HP%, какие фиты есть). GrantItem-правила (фит даёт
    // доступ к другому фиту/действию) перенесены в `grants` — отображаются на листе персонажа
    // текстом "Даёт: ...", без автодобавления как отдельного элемента (упростили: система не
    // ведёт учёт "откуда взялся" фит/действие).
    private static IEnumerable<RuleEntry> BuildFeats(GameSystemId systemId)
    {
        var seeds = LoadEmbeddedJson<SeedFeat>("pf2e-feats.json");
        return seeds.Select(f => RuleEntry.Create(
            systemId, RuleCategory.Feat, f.Slug, f.Name,
            summary: $"{f.Level} уровень · {CategoryLabel(f.Category)}",
            contentMarkdown: f.Description,
            statsJson: JsonSerializer.Serialize(new
            {
                level = f.Level, category = f.Category, action_type = f.ActionType, actions = f.Actions,
                traits = f.Traits, prerequisites = f.Prerequisites,
                modifiers = f.Modifiers.Select(m => new { selector = m.Selector, value = m.Value, type = m.Type, predicate = m.Predicate }),
                grants = f.Grants,
            }),
            tags: ["фит", "PF2e", f.Category], isHomebrew: false,
            source: $"{f.Source} (Foundry pf2e system data, ORC)"));
    }

    private static string CategoryLabel(string category) => category switch
    {
        "ancestry" => "предковый",
        "class" => "классовый",
        "classfeature" => "классовая способность",
        "skill" => "навыковый",
        "general" => "общий",
        "bonus" => "бонусный",
        _ => category,
    };

    private static List<T> LoadEmbeddedJson<T>(string fileName)
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
        if (resourceName is null)
            return [];
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return [];
        return JsonSerializer.Deserialize<List<T>>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? [];
    }

    // ── Снаряжение — реальные официальные данные Paizo (ORC), см. Seeding/Data/README.md ──

    private sealed record SeedEquipment(
        string Slug, string Name, string ItemType, int Level, double PriceGp, object? Bulk,
        string Traits, string Description, Dictionary<string, object?> Extra, string Source);

    private static IEnumerable<RuleEntry> BuildEquipment(GameSystemId systemId)
    {
        var seeds = LoadEmbeddedJson<SeedEquipment>("pf2e-equipment.json");
        return seeds.Select(e => RuleEntry.Create(
            systemId, RuleCategory.Equipment, e.Slug, e.Name,
            summary: $"{ItemTypeLabel(e.ItemType)} · {e.PriceGp} зм",
            contentMarkdown: e.Description,
            statsJson: JsonSerializer.Serialize(new
            {
                item_kind = e.ItemType, level = e.Level, price_gp = e.PriceGp, bulk = e.Bulk,
                traits = e.Traits.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim().ToLowerInvariant()).Where(t => t.Length > 0).ToArray(),
                extra = e.Extra,
            }),
            tags: ["снаряжение", e.ItemType, "PF2e"], isHomebrew: false,
            source: $"{e.Source} (Foundry pf2e system data, ORC)"));
    }

    // ── Руны — вручную (не из Foundry-выгрузки: та не включает предметы типа "rune" —
    // см. pf2e-equipment.json, только тематические руны вроде "Rune of Sin" попали в выгрузку
    // случайно). Фундаментальные руны потенции/усиления/стойкости обязательны почти для любого
    // PF2e-персонажа выше 1 уровня — без них справочник снаряжения был неполон принципиально,
    // не просто "не самая приоритетная категория". Числа — официальные факты правил (уровень/
    // цена/бонус), не копия текста книги. На английском — RU-источника нет, см. BuildFeats.
    private sealed record SeedRune(string Slug, string Name, string RuneType, int Level, double PriceGp, string Description);

    private static IEnumerable<RuleEntry> BuildRunes(GameSystemId systemId)
    {
        SeedRune[] data =
        [
            new("weapon-potency-1", "Weapon Potency (+1)", "weapon", 2, 35,
                "Etched onto a weapon, grants a +1 item bonus to attack rolls made with it."),
            new("weapon-potency-2", "Weapon Potency (+2)", "weapon", 10, 935,
                "Etched onto a weapon, grants a +2 item bonus to attack rolls made with it."),
            new("weapon-potency-3", "Weapon Potency (+3)", "weapon", 16, 8935,
                "Etched onto a weapon, grants a +3 item bonus to attack rolls made with it."),
            new("striking", "Striking Rune", "weapon", 4, 65,
                "The weapon deals two damage dice instead of one whenever it deals its listed weapon damage."),
            new("greater-striking", "Greater Striking Rune", "weapon", 12, 1065,
                "The weapon deals three damage dice instead of one whenever it deals its listed weapon damage."),
            new("major-striking", "Major Striking Rune", "weapon", 19, 31065,
                "The weapon deals four damage dice instead of one whenever it deals its listed weapon damage."),
            new("armor-potency-1", "Armor Potency (+1)", "armor", 5, 160,
                "Etched onto armor, grants a +1 item bonus to AC."),
            new("armor-potency-2", "Armor Potency (+2)", "armor", 11, 1060,
                "Etched onto armor, grants a +2 item bonus to AC."),
            new("armor-potency-3", "Armor Potency (+3)", "armor", 18, 20560,
                "Etched onto armor, grants a +3 item bonus to AC."),
            new("resilient", "Resilient Rune", "armor", 8, 340,
                "Grants a +1 item bonus to saving throws while the armor is worn."),
            new("greater-resilient", "Greater Resilient Rune", "armor", 14, 3440,
                "Grants a +2 item bonus to saving throws while the armor is worn."),
            new("major-resilient", "Major Resilient Rune", "armor", 20, 49440,
                "Grants a +3 item bonus to saving throws while the armor is worn."),
            new("flaming", "Flaming Rune", "weapon", 8, 500,
                "The weapon deals an extra 1d6 fire damage on a hit, and 1d10 persistent fire damage on a critical hit."),
            new("frost", "Frost Rune", "weapon", 8, 500,
                "The weapon deals an extra 1d6 cold damage on a hit, and 1d10 persistent cold damage on a critical hit."),
            new("shock", "Shock Rune", "weapon", 8, 500,
                "The weapon deals an extra 1d6 electricity damage on a hit, and 1d10 additional electricity damage to the target and adjacent creatures on a critical hit."),
            new("thundering", "Thundering Rune", "weapon", 8, 500,
                "The weapon deals an extra 1d6 sonic damage on a hit; on a critical hit the target must succeed at a Fortitude save or be deafened."),
            new("corrosive", "Corrosive Rune", "weapon", 8, 500,
                "The weapon deals an extra 1d6 acid damage on a hit, and on a critical hit it also deals 1d6 persistent acid damage and can damage the target's own weapon or armor."),
            new("keen", "Keen Rune", "weapon", 13, 3000,
                "The weapon's threat range for a critical hit expands (crits confirm on one more roll on the die than usual)."),
            new("returning", "Returning Rune", "weapon", 3, 55,
                "A thrown weapon flies back to the wielder's hand after the attack, ready to be thrown again."),
            new("ghost-touch", "Ghost Touch Rune", "weapon", 4, 75,
                "The weapon can affect incorporeal creatures normally, ignoring their usual resistance to physical attacks."),
            new("bane", "Bane Rune", "weapon", 4, 66,
                "The wielder can attune the weapon to a chosen creature; it gains a status bonus to attack and damage rolls against that creature (and a lesser bonus against its kind)."),
            new("wounding", "Wounding Rune", "weapon", 7, 340,
                "The weapon deals an extra 1d6 persistent bleed damage on a hit."),
            new("disrupting", "Disrupting Rune", "weapon", 5, 150,
                "The weapon deals an extra 1d6 damage to undead, and on a critical hit the undead target must succeed at a Fortitude save or be destroyed (if it's weak) or stunned."),
            new("fortification-rune", "Fortification Rune", "armor", 12, 2000,
                "The armor gives its wearer a chance to avoid the extra effects of a critical hit against them, treating it as a normal hit instead on a successful flat check."),
        ];

        return data.Select(r => RuleEntry.Create(
            systemId, RuleCategory.Equipment, r.Slug, r.Name,
            summary: $"Руна ({r.RuneType}) · {r.PriceGp} зм · ур. {r.Level}",
            contentMarkdown: r.Description,
            statsJson: JsonSerializer.Serialize(new
            {
                item_kind = "rune", rune_type = r.RuneType, level = r.Level, price_gp = r.PriceGp,
                traits = new[] { "rune", "magical", r.RuneType },
            }),
            tags: ["снаряжение", "рун", "PF2e", "untranslated"], isHomebrew: false,
            source: "PF2e Core Rulebook (hand-authored — Foundry extract does not include fundamental rune items)"));
    }

    private static string ItemTypeLabel(string itemType) => itemType switch
    {
        "weapon" => "оружие",
        "armor" => "доспех",
        "shield" => "щит",
        "consumable" => "расходник",
        "ammo" => "боеприпас",
        "treasure" => "ценность",
        "backpack" => "контейнер",
        "kit" => "набор",
        _ => "снаряжение",
    };

    // ── Бэкграунды ───────────────────────────────────────────────────────────────

    private static IEnumerable<RuleEntry> BuildBackgrounds(GameSystemId systemId)
    {
        (string slug, string name, string boostCodes, string skill, string desc)[] data =
        [
            ("acolyte", "Послушник", "INT,ANY", "Религия",
                "Вы выросли при храме или святилище, изучая писания и ритуалы своей веры."),
            ("criminal", "Преступник", "DEX,ANY", "Плутовство",
                "Вы жили за счёт кражи, контрабанды или другого незаконного промысла."),
            ("farmhand", "Батрак", "CON,ANY", "Природа",
                "Вы выросли, работая на ферме — доите скот, пашете землю, чините изгороди."),
            ("guard", "Стражник", "STR,ANY", "Запугивание",
                "Вы служили стражником — у ворот города, в тюрьме или на посту знатной особы."),
            ("hunter", "Охотник", "WIS,ANY", "Выживание",
                "Вы добывали пропитание охотой и знаете, как выследить дичь в дикой местности."),
            ("noble", "Аристократ", "CHA,ANY", "Общество",
                "Вы выросли в благородной семье, обученные этикету и политике знати."),
            ("scholar", "Учёный", "INT,ANY", "Аркана или Оккультизм",
                "Вы провели годы за книгами в библиотеке или академии, изучая теорию своей области."),
            ("warrior", "Воин", "STR,ANY", "Атлетика",
                "Вы с юных лет обучались владению оружием — в ополчении, банде или военном отряде."),
            ("sailor", "Моряк", "DEX,ANY", "Атлетика",
                "Вы провели годы в море на борту торгового или военного корабля, привыкнув к качке и такелажу."),
            ("herbalist", "Травник", "WIS,ANY", "Природа",
                "Вы собирали и готовили целебные травы для деревенского знахаря или собственной практики."),
            ("gladiator", "Гладиатор", "STR,ANY", "Выступление",
                "Вы сражались на арене ради развлечения толпы, оттачивая зрелищность не меньше эффективности."),
            ("merchant", "Торговец", "INT,ANY", "Общество",
                "Вы вели дела на рынках и караванных путях, изучив толк в оценке товара и переговорах."),
            ("street-urchin", "Беспризорник", "DEX,ANY", "Скрытность",
                "Вы выросли на улицах, выживая смекалкой, воровством по мелочи и знанием подворотен."),
            ("barrister", "Стряпчий", "INT,ANY", "Общество",
                "Вы изучали законы и представляли интересы клиентов в судах и тяжбах."),
        ];

        return data.Select(b => RuleEntry.Create(
            systemId, RuleCategory.Background, b.slug, b.name,
            summary: $"Даёт владение навыком «{b.skill}» и фит первого уровня по бэкграунду",
            contentMarkdown: b.desc,
            statsJson: JsonSerializer.Serialize(new { boost_codes = b.boostCodes.Split(','), skill = b.skill }),
            tags: ["бэкграунд", "PF2e"], isHomebrew: false, source: "PF2e SRD"));
    }
}
