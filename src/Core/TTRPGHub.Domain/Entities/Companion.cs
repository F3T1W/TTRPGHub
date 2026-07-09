using TTRPGHub.Common;

namespace TTRPGHub.Entities;

// N.8 — животные-компаньоны/фамильяры: не полноценные персонажи (нет расы/класса/бэкграунда/
// шести характеристик), а урезанные производные от уровня хозяина. Собственная сущность (не
// jsonb-блоб на Character, как Pf2eStatsJson) — компаньон имеет свою идентичность: отдельно
// отслеживаемые HP, может быть выставлен на стол своим собственным токеном (TokenCombatantType.
// Companion), список компаньонов персонажа нужно листать/удалять по одному. Level не
// пересчитывается автоматически от уровня хозяина (в PF2e компаньоны растут по отдельным
// таблицам класса-хозяина) — ГМ/игрок вписывает текущие статы вручную, как и остальные
// "боевые" поля на листе персонажа (Pf2eAttack, Pf2eResource).
public sealed class Companion : Entity<CompanionId>
{
    public new CompanionId Id { get; private set; }
    public CharacterId OwnerCharacterId { get; private set; }
    public string Name { get; private set; } = "";
    public string Kind { get; private set; } = ""; // "Компаньон" / "Фамильяр" — свободный текст
    public int Level { get; private set; }
    public int MaxHitPoints { get; private set; }
    public int CurrentHitPoints { get; private set; }
    public int? ArmorClass { get; private set; }
    public string? Speed { get; private set; }
    public string? AttacksText { get; private set; }
    public string? AbilitiesText { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime UpdatedAt { get; private set; }

    private Companion() { }

    public static Result<Companion> Create(
        CharacterId ownerCharacterId, string name, string kind, int level,
        int maxHitPoints, int? armorClass, string? speed,
        string? attacksText, string? abilitiesText, string? notes)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation(nameof(Name), "Имя компаньона не может быть пустым.");

        var now = DateTime.UtcNow;
        return new Companion
        {
            Id = CompanionId.New(),
            OwnerCharacterId = ownerCharacterId,
            Name = name.Trim(),
            Kind = string.IsNullOrWhiteSpace(kind) ? "Компаньон" : kind.Trim(),
            Level = level,
            MaxHitPoints = Math.Max(0, maxHitPoints),
            CurrentHitPoints = Math.Max(0, maxHitPoints),
            ArmorClass = armorClass,
            Speed = speed,
            AttacksText = attacksText,
            AbilitiesText = abilitiesText,
            Notes = notes,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public Result Update(
        string name, string kind, int level, int maxHitPoints, int currentHitPoints,
        int? armorClass, string? speed, string? attacksText, string? abilitiesText, string? notes)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation(nameof(Name), "Имя компаньона не может быть пустым.");

        Name = name.Trim();
        Kind = string.IsNullOrWhiteSpace(kind) ? "Компаньон" : kind.Trim();
        Level = level;
        MaxHitPoints = Math.Max(0, maxHitPoints);
        CurrentHitPoints = Math.Clamp(currentHitPoints, 0, MaxHitPoints);
        ArmorClass = armorClass;
        Speed = speed;
        AttacksText = attacksText;
        AbilitiesText = abilitiesText;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
