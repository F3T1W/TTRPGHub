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

        await SeedMissingAsync(system.Id, RuleCategory.Class, BuildClasses(system.Id), "классов", ct);
        await SeedMissingAsync(system.Id, RuleCategory.Race, BuildAncestries(system.Id), "предков (рас)", ct);
    }

    // Раньше здесь была проверка AnyAsync(category) — "если хоть одна запись есть, пропустить
    // весь посев целиком". Это блокировало добавление новых классов/предков в список: на уже
    // засеянной БД массив можно было расширять сколько угодно, ничего не попадало в базу.
    // Проверка по каждому slug отдельно (много ли их — десятки, не тысячи) даёт настоящую
    // идемпотентность: новые записи добавляются, существующие не трогаются и не дублируются.
    private async Task SeedMissingAsync(
        GameSystemId systemId, RuleCategory category, IEnumerable<RuleEntry> candidates, string label, CancellationToken ct)
    {
        var list = candidates.ToList();
        var existingSlugs = (await entryRepository.GetBySlugsAsync(systemId, category, list.Select(e => e.Slug).ToList(), ct))
            .Select(e => e.Slug).ToHashSet();
        var missing = list.Where(e => !existingSlugs.Contains(e.Slug)).ToList();
        if (missing.Count == 0)
            return;

        await entryRepository.AddRangeAsync(missing, ct);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Добавлено {Count} новых {Label} Pathfinder 2e", missing.Count, label);
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
            ("bard", "Бард", "Харизма", ["CHA"], 8,
                "Заклинатель-исполнитель, черпающий магию из выступления. Творит окультные заклинания спонтанно и поддерживает союзников композициями.",
                "Музы (источник вдохновения определяет бонусные заклинания), окультное спонтанное заклинательство, композиционные заклинания как выступления."),
            ("druid", "Друид", "Мудрость", ["WIS"], 8,
                "Хранитель баланса природы. Творит первородные заклинания и обладает одним из орденов друидов, дающим уникальное умение природы.",
                "Орден друида (дикий облик, лидер стаи, штормовой или лесной), первородное подготовленное заклинательство, табу ордена."),
            ("monk", "Монах", "Сила или Ловкость", ["STR", "DEX"], 10,
                "Мастер боевых искусств, сражающийся без оружия и брони, используя внутреннюю энергию Ки для сверхчеловеческих подвигов.",
                "Безоружные удары как оружие с высокой костью урона, поток Ки (Ki), стойка боевых искусств."),
            ("champion", "Чемпион", "Сила", ["STR"], 10,
                "Воин, давший священную клятву своему делу или божеству. Реакция «Возмездие чемпиона» защищает союзников ценой урона по себе.",
                "Причина чемпиона (Искупление/Свобода/Справедливость и т.д.) определяет реакцию, божественный союзник, кодекс чести."),
            ("sorcerer", "Чародей", "Харизма", ["CHA"], 6,
                "Заклинатель с врождённой магией, текущей по родословной. Творит заклинания спонтанно из одной из традиций в зависимости от происхождения.",
                "Родословная определяет традицию заклинаний и бонусные заклинания, спонтанное заклинательство, магический очаг силы родословной."),
            ("alchemist", "Алхимик", "Интеллект", ["INT"], 8,
                "Изобретатель, создающий на лету алхимические предметы — бомбы, эликсиры и мутагены — из пропитанных магией реагентов.",
                "Пропитанные реагенты (создаваемые ежедневно алхимические предметы), исследование алхимика (бомбы/мутагены/эликсиры/поле хирурга), быстрая алхимия."),

            // Ниже — классы, добавленные без RU-источника (по прямому запросу пользователя:
            // "если чего-то нет на русском, добавляй на английском, переводами займусь позже").
            // Текст написан своими словами (не дословная цитата книги правил), как и остальные
            // записи в этом массиве — только факты (ключевая характеристика, HP, суть механики).
            ("investigator", "Investigator", "Intelligence", ["INT"], 8,
                "A methodical detective who studies a situation before acting, using Devise a Stratagem to line up a precise attack based on careful observation.",
                "Devise a Stratagem (bonus to a planned attack after analyzing the situation), methodology (forensic medicine/empiricism/etc. shapes investigative style), keen recollection."),
            ("magus", "Magus", "Intelligence", ["INT"], 8,
                "A spellcaster-warrior who fuses weapon strikes and arcane spells into a single devastating action via Spellstrike.",
                "Spellstrike (channel a spell through a weapon strike), arcane cantrips, spell combat, hybrid study specialization."),
            ("oracle", "Oracle", "Charisma", ["CHA"], 8,
                "A spontaneous divine spellcaster bound to a mystery granting unique revelations, balanced by a curse that worsens over time.",
                "Mystery (defines bonus spells and revelations), oracular curse (escalating drawback with growing benefits), spontaneous divine spellcasting."),
            ("swashbuckler", "Swashbuckler", "Dexterity", ["DEX"], 8,
                "A flashy duelist who builds Panache through daring deeds and spends it to fuel finishing blows.",
                "Panache (resource gained from stylish combat actions), finisher techniques, swashbuckler style (defines how Panache is earned)."),
            ("thaumaturge", "Thaumaturge", "Charisma", ["CHA"], 8,
                "An occult investigator who channels a bonded implement to exploit the weaknesses of monsters and spirits.",
                "Esoteric lore (broad knowledge check for supernatural threats), implements (tome/lantern/mirror/etc. each grant unique exploits), first implement bond."),
            ("witch", "Witch", "Intelligence", ["INT"], 6,
                "A spellcaster granted arcane secrets by a mysterious patron, embodied in a familiar that carries the patron's will.",
                "Patron theme (defines granted hex spells and lessons), familiar (required, carries patron abilities), prepared spellcasting."),
            ("gunslinger", "Gunslinger", "Dexterity", ["DEX"], 10,
                "A firearms and crossbow specialist who relies on a signature way of fighting (a way of the gun) to land precise, powerful shots.",
                "Way of the gun (defines fighting style and reload bonus), singular expertise (deadly aim with one chosen weapon), reloading actions."),
            ("inventor", "Inventor", "Intelligence", ["INT"], 8,
                "A tinkerer who builds a signature innovation — an armor, weapon, or construct companion — and improvises breakthroughs mid-combat.",
                "Innovation (the constructed device that defines the class), overdrive (risk a malfunction for a burst of power), unstable actions."),
            ("summoner", "Summoner", "Charisma", ["CHA"], 10,
                "Bonded permanently to an eidolon — a semi-independent creature that fights alongside (and shares actions with) the summoner.",
                "Eidolon (linked creature sharing the summoner's turn), shared HP pool with the eidolon, tandem spellcasting."),
            ("psychic", "Psychic", "Intelligence, Wisdom, or Charisma", ["INT", "WIS", "CHA"], 8,
                "A spontaneous occult spellcaster whose mind can overflow into an Unleash Psyche state, boosting cantrips at the cost of exposing the psyche to attack.",
                "Unleash Psyche (empowered cantrip once per turn while active, but weakens mental defenses), subconscious mind (defines bonus spells), amped cantrips."),
            ("kineticist", "Kineticist", "Constitution", ["CON"], 8,
                "Channels raw elemental power (air/earth/fire/water/wood/metal) directly through the body instead of casting traditional spells.",
                "Elemental gates (chosen elements determine available impulses), impulses (at-will elemental abilities, no spell slots), kinetic aura."),
            ("guardian", "Guardian", "Strength", ["STR"], 10,
                "A frontline protector who plants a defensive stance and punishes enemies who ignore the ally standing behind them.",
                "Guardian's stance (defensive posture with reactive punishment), block, taunt-style aggro mechanics protecting allies."),
            ("exemplar", "Exemplar", "Strength or Dexterity", ["STR", "DEX"], 10,
                "A mortal touched by divine spark, channeling legendary feats of heroism through personal relics called ikons.",
                "Ikons (personal legendary items that grow in power), transcendence (surge of divine might), spark (fuels ikon abilities)."),
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
            tags: UntranslatedClassSlugs.Contains(c.slug) ? ["класс", "PF2e", "untranslated"] : ["класс", "PF2e"],
            isHomebrew: false, source: "PF2e SRD"));
    }

    // Классы, добавленные без RU-перевода (см. комментарий в BuildClasses) — помечены тегом
    // "untranslated", чтобы будущий перевод мог найти их без ручного перебора.
    private static readonly HashSet<string> UntranslatedClassSlugs =
        ["investigator", "magus", "oracle", "swashbuckler", "thaumaturge", "witch",
         "gunslinger", "inventor", "summoner", "psychic", "kineticist", "guardian", "exemplar"];

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
            ("half-elf", "Полуэльф", 8, "Среднее", 30, "Любые два (на выбор)", ["ANY", "ANY"], "Нет", null,
                "Потомки людей и эльфов, редко встречающие себе подобных. Совмещают долголетие эльфов с приспособляемостью людей."),
            ("goblin", "Гоблин", 6, "Маленькое", 25, "Ловкость, Харизма", ["DEX", "CHA"], "Мудрость", "WIS",
                "Маленький, шумный и находчивый народ с тёмным зрением и вкусом к разрушению — и совершенно незаслуженной репутацией дикарей."),
            ("orc", "Орк", 10, "Среднее", 25, "Сила, любая", ["STR", "ANY"], "Нет", null,
                "Крепкий и выносливый народ с тёмным зрением, способный процветать в самых суровых землях. Не менее разнообразны характером, чем любой другой народ."),
            ("tiefling", "Тифлинг", 8, "Среднее", 25, "Интеллект, Харизма", ["INT", "CHA"], "Нет", null,
                "Потомки существ, отмеченных договором с потусторонними силами — рогами, хвостом или иными чертами наследия, часто демонического или инфернального происхождения."),
            ("gnoll", "Гнолл", 8, "Среднее", 25, "Сила, Мудрость", ["STR", "WIS"], "Интеллект", "INT",
                "Народ гиеноподобных гуманоидов, объединённый стайными узами и почитанием силы. Изгнанники часто находят новый дом среди других рас."),
            ("hobgoblin", "Хобгоблин", 8, "Среднее", 25, "Телосложение, Интеллект", ["CON", "INT"], "Харизма", "CHA",
                "Дисциплинированный и организованный народ, ценящий порядок и мастерство ремесла выше всего — включая военное дело."),

            // Ниже — предки, добавленные без RU-перевода (см. комментарий в BuildClasses про
            // "англ. пока нет RU-источника"). Size/speed указаны на английском ("Small"/"Medium")
            // — маппинг size→числовой код в конце файла учитывает оба варианта написания.
            ("catfolk", "Catfolk", 6, "Small", 25, "Dexterity, Charisma", ["DEX", "CHA"], "Wisdom", "WIS",
                "A curious, agile people with feline features and a taste for adventure — quick reflexes and an even quicker wit."),
            ("kobold", "Kobold", 6, "Small", 25, "Dexterity, Charisma", ["DEX", "CHA"], "Constitution", "CON",
                "Small dragon-kin with a talent for traps and tunnels, blessed with darkvision and a fierce sense of clan loyalty."),
            ("lizardfolk", "Lizardfolk", 8, "Medium", 25, "Strength, Wisdom", ["STR", "WIS"], "Intelligence", "INT",
                "A reptilian people at home in swamp and jungle alike, strong swimmers who value pragmatism and the natural order."),
            ("ratfolk", "Ratfolk", 6, "Small", 25, "Dexterity, Intelligence", ["DEX", "INT"], "Strength", "STR",
                "A resourceful, communal people with keen senses and a knack for tinkering, thriving wherever they settle."),
            ("leshy", "Leshy", 8, "Small", 25, "Constitution, Wisdom", ["CON", "WIS"], "Charisma", "CHA",
                "A plant creature grown from a magic seed, needing regular watering to stay healthy — tied closely to the natural world that made it."),
            ("fetchling", "Fetchling", 8, "Medium", 25, "Dexterity, Intelligence", ["DEX", "INT"], "Charisma", "CHA",
                "Humanoids touched by the Shadow Plane, comfortable in darkness and possessing low-light vision inherited from their ancestry."),
            ("automaton", "Automaton", 8, "Medium", 25, "Any two (free)", ["ANY", "ANY"], "Нет", null,
                "A living construct animated by a soul bound into a mechanical body, immune to many conditions that trouble living creatures."),
            ("kitsune", "Kitsune", 6, "Small", 25, "Dexterity, Charisma", ["DEX", "CHA"], "Constitution", "CON",
                "A shapechanging people descended from fox spirits, able to take a human-like form while retaining fox-like features and instincts."),
            ("grippli", "Grippli", 6, "Small", 25, "Dexterity, Wisdom", ["DEX", "WIS"], "Strength", "STR",
                "A frog-like people at home in swamps and jungle canopies, natural climbers and jumpers with a talent for camouflage."),
            ("azarketi", "Azarketi", 8, "Medium", 25, "Constitution, Wisdom", ["CON", "WIS"], "Intelligence", "INT",
                "An amphibious coastal people equally at home on land or underwater, with a swim speed and a deep connection to the sea."),
        ];

        return data.Select(a => RuleEntry.Create(
            systemId, RuleCategory.Race, a.slug, a.name,
            summary: $"{a.size} · Скорость {a.speed} фт. · HP предка: {a.hp}",
            contentMarkdown: a.desc,
            statsJson: JsonSerializer.Serialize(new
            {
                hp = a.hp,
                size = a.size.StartsWith("Мал", StringComparison.Ordinal) || a.size.Equals("Small", StringComparison.OrdinalIgnoreCase) ? 1 : 2,
                speed = a.speed,
                boosts = a.boosts,
                boost_codes = a.boostCodes,
                flaw = a.flaw,
                flaw_code = a.flawCode,
                traits = new[] { a.slug, "humanoid" },
            }),
            tags: UntranslatedAncestrySlugs.Contains(a.slug) ? ["раса", "PF2e", "untranslated"] : ["раса", "PF2e"],
            isHomebrew: false, source: "PF2e SRD"));
    }

    // Предки, добавленные без RU-перевода (см. UntranslatedClassSlugs выше — тот же принцип).
    private static readonly HashSet<string> UntranslatedAncestrySlugs =
        ["catfolk", "kobold", "lizardfolk", "ratfolk", "leshy", "fetchling",
         "automaton", "kitsune", "grippli", "azarketi"];
}
