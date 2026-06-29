using TTRPGHub.Common;

namespace TTRPGHub.Entities.Dnd5e;

public sealed class Dnd5eSpell : Entity<Dnd5eSpellId>
{
    public new Dnd5eSpellId Id { get; private set; }
    public string Slug { get; private set; } = "";
    public string Name { get; private set; } = "";
    public int Level { get; private set; }
    public string School { get; private set; } = "";
    public string CastingTime { get; private set; } = "";
    public string Range { get; private set; } = "";
    public string Components { get; private set; } = "";
    public string Duration { get; private set; } = "";
    public bool Concentration { get; private set; }
    public bool Ritual { get; private set; }
    public string Description { get; private set; } = "";
    public string? HigherLevel { get; private set; }
    public string Classes { get; private set; } = "";
    public string? Material { get; private set; }
    public string Source { get; private set; } = "SRD";

    private Dnd5eSpell() { }

    public static Dnd5eSpell Create(
        string slug, string name, int level, string school,
        string castingTime, string range, string components,
        string duration, bool concentration, bool ritual,
        string description, string? higherLevel, string classes,
        string? material, string source)
    {
        return new Dnd5eSpell
        {
            Id          = Dnd5eSpellId.New(),
            Slug        = slug,
            Name        = name,
            Level       = level,
            School      = school,
            CastingTime = castingTime,
            Range       = range,
            Components  = components,
            Duration    = duration,
            Concentration = concentration,
            Ritual      = ritual,
            Description = description,
            HigherLevel = higherLevel,
            Classes     = classes,
            Material    = material,
            Source      = source,
        };
    }
}
