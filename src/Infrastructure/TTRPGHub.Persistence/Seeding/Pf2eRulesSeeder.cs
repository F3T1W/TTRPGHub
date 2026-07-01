using System.Text.Json;
using Microsoft.Extensions.Logging;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Seeding;

// PF2e-контент пишется вручную сразу на русском (как и Pf2eImporter для спеллов/монстров) —
// переводчик не нужен, официального открытого API с PF2e-классами/расами на русском нет.
// Механика PF2e (boosts/flaws характеристик, ранги владения, HP = предок + класс·уровень)
// принципиально отличается от D&D5e — мастер создания персонажа поддерживает базовую автоматику
// через Pf2eCharacterAutomationCalculator (упрощённо: буст предка+класса, HP, AC при условной
// тренированной защите без доспеха), см. ROADMAP.md за деталями и известными упрощениями.
public sealed class Pf2eRulesSeeder(
    IGameSystemRepository systemRepository,
    IRuleEntryRepository entryRepository,
    IUnitOfWork unitOfWork,
    ILogger<Pf2eRulesSeeder> logger)
{
    public async Task SeedIfEmptyAsync(CancellationToken ct = default)
    {
        var system = await systemRepository.GetBySlugAsync("pf2e", ct);
        if (system is null)
        {
            logger.LogWarning("Система pf2e ещё не создана — пропускаю посев классов/предков PF2e");
            return;
        }

        if (!await entryRepository.AnyAsync(system.Id, RuleCategory.Class, ct))
        {
            await entryRepository.AddRangeAsync(BuildClasses(system.Id), ct);
            await unitOfWork.SaveChangesAsync(ct);
            logger.LogInformation("Добавлены классы Pathfinder 2e");
        }

        if (!await entryRepository.AnyAsync(system.Id, RuleCategory.Race, ct))
        {
            await entryRepository.AddRangeAsync(BuildAncestries(system.Id), ct);
            await unitOfWork.SaveChangesAsync(ct);
            logger.LogInformation("Добавлены предки (расы) Pathfinder 2e");
        }
    }

    private static IEnumerable<RuleEntry> BuildClasses(GameSystemId systemId)
    {
        // key_ability_codes — структурированный код(ы) ключевой характеристики для автоматики
        // (CharacterAutomationCalculator.Pf2e), key_ability — то же самое человекочитаемым текстом
        // для отображения в справочнике. Классы с выбором (Боец/Следопыт) дают игроку оба варианта —
        // автоматика применяет бонус к первому коду, второй остаётся для ручного выбора игроком.
        (string slug, string name, string keyAbility, string[] keyAbilityCodes, int hpPerLevel, string desc, string features)[] data =
        [
            ("fighter", "Боец", "Сила или Ловкость", ["STR", "DEX"], 10,
                "Мастер оружия и тактики ближнего боя. Наносит больше урона обычными атаками, чем любой другой класс, и превосходно владеет разнообразным снаряжением.",
                "Атака мастера на 1 уровне, реакция «Стойкий защитник», доступ почти ко всем видам оружия и брони."),
            ("wizard", "Волшебник", "Интеллект", ["INT"], 6,
                "Учёный маг, изучающий заклинания по книге заклинаний. Специализируется в одной из магических школ и обладает уникальным заклинанием-фокусом.",
                "Заклинания подготавливаются заранее, магическая школа даёт бонусное заклинание, арканный жезл на 1 уровне."),
            ("rogue", "Плут", "Ловкость (обычно)", ["DEX"], 8,
                "Мастер скрытности и точных ударов. Наносит дополнительный урон от превосходства (Sneak Attack), когда враг ослаблен или отвлечён.",
                "Превосходство (доп. урон при преимуществе в ситуации), уловки плута, экспертное владение отражённое в высокой Ловкости."),
            ("cleric", "Жрец", "Мудрость", ["WIS"], 8,
                "Проводник силы божества. Творит божественные заклинания и обладает уникальным доменным заклинанием, отражающим природу его бога.",
                "Божественный фокус, доменное заклинание, спонтанное или подготовленное чтение заклинаний на выбор."),
            ("ranger", "Следопыт", "Сила или Ловкость", ["STR", "DEX"], 10,
                "Охотник и следопыт, который метит цель и обрушивает на неё сфокусированный урон, попутно ориентируясь в дикой природе.",
                "Охотничья метка (доп. урон по помеченной цели), выбор боевого стиля (стрельба или двойное оружие)."),
            ("barbarian", "Варвар", "Сила", ["STR"], 12,
                "Воин, черпающий силу из первобытной ярости. Во время неистовства получает временные хиты и бонус к урону, но теряет часть контроля.",
                "Неистовство (бонус к урону и временные хиты, штраф к КЗ), инстинкт неистовства определяет стиль боя."),
        ];

        return data.Select(c => RuleEntry.Create(
            systemId, RuleCategory.Class, c.slug, c.name,
            summary: $"Ключевая характеристика: {c.keyAbility} · HP за уровень: {c.hpPerLevel}",
            contentMarkdown: c.desc,
            statsJson: JsonSerializer.Serialize(new
            {
                key_ability = c.keyAbility,
                key_ability_codes = c.keyAbilityCodes,
                hp_per_level = c.hpPerLevel,
                class_features = c.features
            }),
            tags: ["класс", "PF2e"], isHomebrew: false, source: "PF2e SRD"));
    }

    private static IEnumerable<RuleEntry> BuildAncestries(GameSystemId systemId)
    {
        // boost_codes/flaw_code — структурированные коды для автоматики; "ANY" означает свободный
        // выбор игрока (человек/полуорк) — автоматика такие бонусы не применяет, только явные пары.
        // boosts/flaw — то же текстом для отображения в справочнике.
        (string slug, string name, int hp, string size, int speed, string boosts, string[] boostCodes, string flaw, string? flawCode, string desc)[] data =
        [
            ("human", "Человек", 8, "Среднее", 25, "Любые два (на выбор)", ["ANY", "ANY"], "Нет", null,
                "Самый распространённый и разносторонний народ. Универсальность — их главное преимущество: человек может стать кем угодно."),
            ("elf", "Эльф", 6, "Среднее", 30, "Ловкость, Интеллект", ["DEX", "INT"], "Телосложение", "CON",
                "Долгоживущий народ с острым умом и связью с магией. Эльфы медленно взрослеют, но живут столетиями, копя знания и опыт."),
            ("dwarf", "Дварф", 10, "Среднее", 20, "Телосложение, Мудрость", ["CON", "WIS"], "Харизма", "CHA",
                "Крепкий подземный народ, славящийся стойкостью и мастерством кузнецов. Обладают тёмным зрением и сопротивлением яду."),
            ("halfling", "Полурослик", 6, "Маленькое", 25, "Ловкость, Мудрость", ["DEX", "WIS"], "Сила", "STR",
                "Небольшой, удачливый и осторожный народ. Полурослики славятся везением — их особый талант позволяет перебрасывать неудачные броски."),
            ("gnome", "Гном", 8, "Маленькое", 25, "Телосложение, Харизма", ["CON", "CHA"], "Сила", "STR",
                "Любопытный народ с сильной связью с Первым Миром. Гномы обладают врождённой магией и почти неистощимым любопытством."),
            ("half-orc", "Полуорк", 8, "Среднее", 25, "Любые два (на выбор)", ["ANY", "ANY"], "Нет", null,
                "Потомки людей и орков, сочетающие силу и выносливость обоих народов. Часто становятся изгоями в обоих обществах, что закаляет характер."),
        ];

        return data.Select(a => RuleEntry.Create(
            systemId, RuleCategory.Race, a.slug, a.name,
            summary: $"{a.size} · Скорость {a.speed} фт. · HP предка: {a.hp}",
            contentMarkdown: a.desc,
            statsJson: JsonSerializer.Serialize(new
            {
                hp = a.hp,
                size = a.size,
                speed = a.speed,
                boosts = a.boosts,
                boost_codes = a.boostCodes,
                flaw = a.flaw,
                flaw_code = a.flawCode
            }),
            tags: ["раса", "PF2e"], isHomebrew: false, source: "PF2e SRD"));
    }
}
