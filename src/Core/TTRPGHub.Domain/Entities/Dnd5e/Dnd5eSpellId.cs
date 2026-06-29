namespace TTRPGHub.Entities.Dnd5e;

public readonly record struct Dnd5eSpellId(Guid Value)
{
    public static Dnd5eSpellId New() => new(Guid.NewGuid());
}
