using TTRPGHub.Common;

namespace TTRPGHub.Entities.Dnd5e;

public sealed class Dnd5eMonster : Entity<Dnd5eMonsterId>
{
    public new Dnd5eMonsterId Id { get; private set; }
    public string Slug { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string Size { get; private set; } = "";
    public string Type { get; private set; } = "";
    public string? Subtype { get; private set; }
    public string Alignment { get; private set; } = "";
    public int ArmorClass { get; private set; }
    public string? ArmorDesc { get; private set; }
    public int HitPoints { get; private set; }
    public string HitDice { get; private set; } = "";
    public string Speed { get; private set; } = "";
    public int Strength { get; private set; }
    public int Dexterity { get; private set; }
    public int Constitution { get; private set; }
    public int Intelligence { get; private set; }
    public int Wisdom { get; private set; }
    public int Charisma { get; private set; }
    public string ChallengeRating { get; private set; } = "";
    public int Xp { get; private set; }
    public string? SenseStr { get; private set; }
    public string? LanguagesStr { get; private set; }
    public string? Actions { get; private set; }
    public string? SpecialAbilities { get; private set; }
    public string? Reactions { get; private set; }
    public string? LegendaryActions { get; private set; }
    public string Source { get; private set; } = "SRD";

    private Dnd5eMonster() { }

    public static Dnd5eMonster Create(
        string slug, string name, string size, string type, string? subtype,
        string alignment, int armorClass, string? armorDesc, int hitPoints, string hitDice,
        string speed, int str, int dex, int con, int intel, int wis, int cha,
        string challengeRating, int xp, string? senses, string? languages,
        string? actions, string? specialAbilities, string? reactions,
        string? legendaryActions, string source)
    {
        return new Dnd5eMonster
        {
            Id                = Dnd5eMonsterId.New(),
            Slug              = slug,
            Name              = name,
            Size              = size,
            Type              = type,
            Subtype           = subtype,
            Alignment         = alignment,
            ArmorClass        = armorClass,
            ArmorDesc         = armorDesc,
            HitPoints         = hitPoints,
            HitDice           = hitDice,
            Speed             = speed,
            Strength          = str,
            Dexterity         = dex,
            Constitution      = con,
            Intelligence      = intel,
            Wisdom            = wis,
            Charisma          = cha,
            ChallengeRating   = challengeRating,
            Xp                = xp,
            SenseStr          = senses,
            LanguagesStr      = languages,
            Actions           = actions,
            SpecialAbilities  = specialAbilities,
            Reactions         = reactions,
            LegendaryActions  = legendaryActions,
            Source            = source,
        };
    }
}
