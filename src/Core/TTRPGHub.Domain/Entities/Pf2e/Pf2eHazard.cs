using TTRPGHub.Common;

namespace TTRPGHub.Entities.Pf2e;

// N.1 — hazards (ловушки/опасности) как отдельная категория контента, которой не было вообще
// (не существо и не предмет — своя механика обнаружения по Скрытности вместо Восприятия,
// отдельная процедура обезвреживания, твёрдость/ОЗ самого механизма). Источник данных —
// pf2e-ru-translation (см. M.4 и /licenses): в отличие от монстров/заклинаний (английский
// ORC-дамп + RU-оверлей поверх), у хазардов нет предзагруженного английского набора вообще —
// русский текст здесь единственный источник контента, не оверлей.
public sealed class Pf2eHazard : Entity<Pf2eHazardId>
{
    public new Pf2eHazardId Id { get; private set; }
    public string Slug { get; private set; } = "";
    public string Name { get; private set; } = ""; // английское — для консистентности слагов/поиска
    public string NameRu { get; private set; } = "";
    public int Level { get; private set; }
    public string Traits { get; private set; } = "";
    public int StealthDc { get; private set; }
    public string? StealthNote { get; private set; } // "(обучен)" и т.п. — минимальный ранг мастерства
    public string? Description { get; private set; }
    public string? DisableText { get; private set; }
    public int? ArmorClass { get; private set; }
    public int? Fortitude { get; private set; }
    public int? Reflex { get; private set; }
    public int? Hardness { get; private set; }
    public int? HitPoints { get; private set; }
    public string? Immunities { get; private set; }
    public string? AbilitiesText { get; private set; } // реакции/атаки/ауры — единым форматированным блоком
    public string? ResetText { get; private set; }
    public string Source { get; private set; } = "";

    private Pf2eHazard() { }

    public static Pf2eHazard Create(
        string slug, string name, string nameRu, int level, string traits,
        int stealthDc, string? stealthNote, string? description, string? disableText,
        int? armorClass, int? fortitude, int? reflex, int? hardness, int? hitPoints,
        string? immunities, string? abilitiesText, string? resetText, string source)
    {
        return new Pf2eHazard
        {
            Id = Pf2eHazardId.New(),
            Slug = slug,
            Name = name,
            NameRu = nameRu,
            Level = level,
            Traits = traits,
            StealthDc = stealthDc,
            StealthNote = stealthNote,
            Description = description,
            DisableText = disableText,
            ArmorClass = armorClass,
            Fortitude = fortitude,
            Reflex = reflex,
            Hardness = hardness,
            HitPoints = hitPoints,
            Immunities = immunities,
            AbilitiesText = abilitiesText,
            ResetText = resetText,
            Source = source,
        };
    }
}
