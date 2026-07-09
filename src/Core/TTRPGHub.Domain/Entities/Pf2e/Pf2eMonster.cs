using TTRPGHub.Common;

namespace TTRPGHub.Entities.Pf2e;

public sealed class Pf2eMonster : Entity<Pf2eMonsterId>
{
    public new Pf2eMonsterId Id { get; private set; }
    public string Slug { get; private set; } = "";
    public string Name { get; private set; } = "";
    public int Level { get; private set; }
    public string Size { get; private set; } = "";
    public string Traits { get; private set; } = "";
    public int Perception { get; private set; }
    public string? Senses { get; private set; }
    public string? Languages { get; private set; }
    public string? Skills { get; private set; }
    public int Strength { get; private set; }
    public int Dexterity { get; private set; }
    public int Constitution { get; private set; }
    public int Intelligence { get; private set; }
    public int Wisdom { get; private set; }
    public int Charisma { get; private set; }
    public int ArmorClass { get; private set; }
    public int Fortitude { get; private set; }
    public int Reflex { get; private set; }
    public int Will { get; private set; }
    public int HitPoints { get; private set; }
    public string Speed { get; private set; } = "";
    public string? Attacks { get; private set; }
    public string? Abilities { get; private set; }
    public string Source { get; private set; } = "PF2e SRD";

    // Структурированные атаки для автоматизации кнопок "Атаковать"/"Урон" на токене монстра
    // (см. H.8 — то же для персонажей). null = у монстра есть только текстовое поле Attacks
    // (старый контент, ещё не переструктурирован) — кнопки автоматизации не показываются,
    // текст всё равно виден в статблоке. Формат — JSON-массив объектов
    // {name, bonus, damageDice, damageBonus, damageType}, бонус атаки уже готовое число из
    // статблока (не считается из ранга+уровня+характеристики, как у персонажей).
    public string? AttacksJson { get; private set; }

    // J.2 (combat tracker "до идеального состояния") — сопротивления/уязвимости из статблока
    // для расчёта эффективного урона на токене (см. Table.razor.cs ApplyDamageAsync). Формат —
    // JSON-массив {type, value, exceptions[]}: value может быть отрицательным по правилам PF2e
    // не бывает — уязвимость и сопротивление хранятся в раздельных полях, а не одним знаком.
    public string? ResistancesJson { get; private set; }
    public string? WeaknessesJson { get; private set; }

    // N.4 — иммунитеты (тот же формат {type, exceptions[]}, без value — иммунитет либо есть,
    // либо нет, в отличие от сопротивления/уязвимости с числовой величиной). Тип может быть
    // как типом урона ("fire", "poison"), так и состоянием ("frightened", "paralyzed") —
    // Foundry хранит оба вида в одном списке immunities, различий по структуре нет.
    public string? ImmunitiesJson { get; private set; }

    // N.7 — ауры (эффект, автоматически применяемый ко всем токенам в радиусе — не только
    // текст в Abilities). Формат — JSON-массив {radiusFeet, effectSlug, effectName, value}:
    // effectSlug — тот же слаг состояния, что и у ApplyTokenCondition (N.4/L.2), value —
    // опциональная величина состояния (например frightened 1). Одна аура = один эффект;
    // монстр с несколькими аурами (редкость) — несколько записей в массиве.
    public string? AurasJson { get; private set; }

    private Pf2eMonster() { }

    public static Pf2eMonster Create(
        string slug, string name, int level, string size, string traits,
        int perception, string? senses, string? languages, string? skills,
        int str, int dex, int con, int intel, int wis, int cha,
        int armorClass, int fortitude, int reflex, int will, int hitPoints,
        string speed, string? attacks, string? abilities, string source,
        string? attacksJson = null, string? resistancesJson = null, string? weaknessesJson = null,
        string? immunitiesJson = null, string? aurasJson = null)
    {
        return new Pf2eMonster
        {
            Id           = Pf2eMonsterId.New(),
            Slug         = slug,
            Name         = name,
            Level        = level,
            Size         = size,
            Traits       = traits,
            Perception   = perception,
            Senses       = senses,
            Languages    = languages,
            Skills       = skills,
            Strength     = str,
            Dexterity    = dex,
            Constitution = con,
            Intelligence = intel,
            Wisdom       = wis,
            Charisma     = cha,
            ArmorClass   = armorClass,
            Fortitude    = fortitude,
            Reflex       = reflex,
            Will         = will,
            HitPoints    = hitPoints,
            Speed        = speed,
            Attacks      = attacks,
            Abilities    = abilities,
            Source       = source,
            AttacksJson  = attacksJson,
            ResistancesJson = resistancesJson,
            WeaknessesJson  = weaknessesJson,
            ImmunitiesJson  = immunitiesJson,
            AurasJson       = aurasJson,
        };
    }
}
