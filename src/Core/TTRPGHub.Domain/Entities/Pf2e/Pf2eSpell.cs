using TTRPGHub.Common;

namespace TTRPGHub.Entities.Pf2e;

public sealed class Pf2eSpell : Entity<Pf2eSpellId>
{
    public new Pf2eSpellId Id { get; private set; }
    public string Slug { get; private set; } = "";
    public string Name { get; private set; } = "";
    public int Level { get; private set; }
    public string Traditions { get; private set; } = "";
    public string Traits { get; private set; } = "";
    public string Cast { get; private set; } = "";
    public string? Range { get; private set; }
    public string? Area { get; private set; }
    public string? Targets { get; private set; }
    public string Duration { get; private set; } = "";
    public string Description { get; private set; } = "";
    public string? Heightened { get; private set; }
    public string? DamageJson { get; private set; }
    public string? HeighteningJson { get; private set; }
    public string? DefenseJson { get; private set; }
    public string Source { get; private set; } = "PF2e SRD";

    private Pf2eSpell() { }

    public static Pf2eSpell Create(
        string slug, string name, int level, string traditions, string traits,
        string cast, string? range, string? area, string? targets, string duration,
        string description, string? heightened, string source,
        string? damageJson = null, string? heighteningJson = null, string? defenseJson = null)
    {
        return new Pf2eSpell
        {
            Id              = Pf2eSpellId.New(),
            Slug            = slug,
            Name            = name,
            Level           = level,
            Traditions      = traditions,
            Traits          = traits,
            Cast            = cast,
            Range           = range,
            Area            = area,
            Targets         = targets,
            Duration        = duration,
            Description     = description,
            Heightened      = heightened,
            DamageJson      = damageJson,
            HeighteningJson = heighteningJson,
            DefenseJson     = defenseJson,
            Source          = source,
        };
    }

    public void SetAutomation(string? damageJson, string? heighteningJson, string? defenseJson)
    {
        DamageJson = damageJson;
        HeighteningJson = heighteningJson;
        DefenseJson = defenseJson;
    }
}
