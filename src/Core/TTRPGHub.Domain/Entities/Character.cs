using TTRPGHub.Common;

namespace TTRPGHub.Entities;

public sealed class Character : Entity<CharacterId>
{
    // Main
    public UserId OwnerId { get; private init; }

    // Co-op — несколько игроков ведут одного персонажа (партийный маскот, наёмный NPC "на
    // подхвате" и т.п.). Совладельцы имеют те же права редактирования, что и владелец, кроме
    // управления самим списком совладельцев — это остаётся только за OwnerId, чтобы не было
    // цепной передачи прав без ведома изначального владельца.
    public List<Guid> CoOwnerIds { get; private set; } = [];

    public string Name { get; private set; } = null!;
    public string Race { get; private set; } = null!;
    public string Class { get; private set; } = null!;
    public int Level { get; private set; }
    public bool IsPublic { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime UpdatedAt { get; private set; }

    // Bio
    public string? Background { get; private set; }
    public string? Alignment { get; private set; }
    public int ExperiencePoints { get; private set; }
    public string? PersonalityTraits { get; private set; }
    public string? Ideals { get; private set; }
    public string? Bonds { get; private set; }
    public string? Flaws { get; private set; }

    // Stats
    public int Strength { get; private set; } = 10;
    public int Dexterity { get; private set; } = 10;
    public int Constitution { get; private set; } = 10;
    public int Intelligence { get; private set; } = 10;
    public int Wisdom { get; private set; } = 10;
    public int Charisma { get; private set; } = 10;

    // Combat Stats
    public int MaxHitPoints { get; private set; }
    public int CurrentHitPoints { get; private set; }
    public int TemporaryHitPoints { get; private set; }
    public int ArmorClass { get; private set; } = 10;
    public int Speed { get; private set; } = 30;
    public string HitDice { get; private set; } = "1d8";

    // Skills
    public List<string> SkillProficiencies { get; private set; } = [];
    public List<string> SavingThrowProficiencies { get; private set; } = [];

    // Traits And Equip
    public string? FeaturesAndTraits { get; private set; }
    public string? Equipment { get; private set; }

    // Profile picture
    public string? AvatarUrl { get; private set; }

    // PF2e-специфика (ранги владения per-skill, ключевая характеристика, class DC, спеллкастинг,
    // структурированный инвентарь) не влезает в плоские D&D5e-колонки выше и не нужна для D&D5e-
    // персонажей — храним отдельным jsonb-блобом по прецеденту RuleEntry.StatsJson, null = обычный
    // D&D5e-персонаж без PF2e-листа.
    public string? Pf2eStatsJson { get; private set; }

    // Структурированные фиты — вместо парсинга FeaturesAndTraits (свободный текст) игрок выбирает
    // фит из справочника по слагу, уровень фиксируется на момент выбора. Свободный текст остаётся
    // как есть — для наследия/импорта и произвольных заметок, эти два поля не связаны.
    public string? SelectedFeatsJson { get; private set; }

    // Calculated props
    public int ProficiencyBonus => Level switch { <= 4 => 2, <= 8 => 3, <= 12 => 4, <= 16 => 5, _ => 6 };
    public int StrengthModifier     => Modifier(Strength);
    public int DexterityModifier    => Modifier(Dexterity);
    public int ConstitutionModifier => Modifier(Constitution);
    public int IntelligenceModifier => Modifier(Intelligence);
    public int WisdomModifier       => Modifier(Wisdom);
    public int CharismaModifier     => Modifier(Charisma);
    public int Initiative           => DexterityModifier;

    private static int Modifier(int score) => (int)Math.Floor((score - 10) / 2.0);

    private Character() { }

    public static Result<Character> Create(
        UserId ownerId,
        string name,
        string race,
        string @class,
        int level)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation(nameof(Name), "Имя персонажа не может быть пустым.");

        if (level is < 1 or > 20)
            return Error.Validation(nameof(Level), "Уровень должен быть от 1 до 20.");

        var now = DateTime.UtcNow;
        return new Character
        {
            Id        = CharacterId.New(),
            OwnerId   = ownerId,
            Name      = name.Trim(),
            Race      = race,
            Class     = @class,
            Level     = level,
            IsPublic  = false,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public Result UpdateSheet(UpdateSheetData data)
    {
        if (string.IsNullOrWhiteSpace(data.Name))
            return Error.Validation(nameof(Name), "Имя персонажа не может быть пустым.");

        if (data.Level is < 1 or > 20)
            return Error.Validation(nameof(Level), "Уровень должен быть от 1 до 20.");

        if (data.ArmorClass < 0)
            return Error.Validation(nameof(ArmorClass), "КД не может быть отрицательным.");

        Name       = data.Name.Trim();
        Race       = data.Race;
        Class      = data.Class;
        Level      = data.Level;
        IsPublic   = data.IsPublic;
        Background = data.Background;
        Alignment  = data.Alignment;
        ExperiencePoints  = data.ExperiencePoints;
        PersonalityTraits = data.PersonalityTraits;
        Ideals     = data.Ideals;
        Bonds      = data.Bonds;
        Flaws      = data.Flaws;

        Strength     = Clamp(data.Strength);
        Dexterity    = Clamp(data.Dexterity);
        Constitution = Clamp(data.Constitution);
        Intelligence = Clamp(data.Intelligence);
        Wisdom       = Clamp(data.Wisdom);
        Charisma     = Clamp(data.Charisma);

        MaxHitPoints       = Math.Max(1, data.MaxHitPoints);
        CurrentHitPoints   = Math.Clamp(data.CurrentHitPoints, 0, data.MaxHitPoints);
        TemporaryHitPoints = Math.Max(0, data.TemporaryHitPoints);
        ArmorClass         = data.ArmorClass;
        Speed              = Math.Max(0, data.Speed);
        HitDice            = string.IsNullOrWhiteSpace(data.HitDice) ? HitDice : data.HitDice.Trim();

        SkillProficiencies      = data.SkillProficiencies;
        SavingThrowProficiencies = data.SavingThrowProficiencies;
        FeaturesAndTraits = data.FeaturesAndTraits;
        Equipment         = data.Equipment;

        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public void SetAvatar(string? url)
    {
        AvatarUrl = url;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPublic(bool isPublic)
    {
        IsPublic  = isPublic;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPf2eStats(string? statsJson)
    {
        Pf2eStatsJson = statsJson;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSelectedFeats(string? selectedFeatsJson)
    {
        SelectedFeatsJson = selectedFeatsJson;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsOwnedBy(UserId userId) => OwnerId == userId || CoOwnerIds.Contains(userId.Value);

    public Result AddCoOwner(Guid userId)
    {
        if (userId == OwnerId.Value)
            return Error.Validation("CoOwnerIds", "Этот пользователь уже владелец персонажа.");
        if (CoOwnerIds.Contains(userId))
            return Error.Validation("CoOwnerIds", "Этот пользователь уже совладелец.");

        CoOwnerIds = [.. CoOwnerIds, userId];
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public void RemoveCoOwner(Guid userId)
    {
        CoOwnerIds = CoOwnerIds.Where(id => id != userId).ToList();
        UpdatedAt = DateTime.UtcNow;
    }

    // Синхронизация HP при бое за столом (TableToken — "привязанный" токен, как связанный actor
    // в Foundry): урон/лечение, применённые к токену игрока, должны отражаться на листе персонажа,
    // а не жить только на копии токена (в отличие от монстров — те намеренно независимы, см. H.4).
    public void SetCurrentHitPoints(int currentHp)
    {
        CurrentHitPoints = Math.Clamp(currentHp, 0, MaxHitPoints);
        UpdatedAt = DateTime.UtcNow;
    }

    private static int Clamp(int score) => Math.Clamp(score, 1, 30);
}

public sealed record UpdateSheetData(
    string Name,
    string Race,
    string Class,
    int Level,
    bool IsPublic,
    string? Background,
    string? Alignment,
    int ExperiencePoints,
    string? PersonalityTraits,
    string? Ideals,
    string? Bonds,
    string? Flaws,
    int Strength,
    int Dexterity,
    int Constitution,
    int Intelligence,
    int Wisdom,
    int Charisma,
    int MaxHitPoints,
    int CurrentHitPoints,
    int TemporaryHitPoints,
    int ArmorClass,
    int Speed,
    string HitDice,
    List<string> SkillProficiencies,
    List<string> SavingThrowProficiencies,
    string? FeaturesAndTraits,
    string? Equipment
);
