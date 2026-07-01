namespace TTRPGHub.Entities.Pf2e;

public readonly record struct Pf2eSpellId(Guid Value)
{
    public static Pf2eSpellId New() => new(Guid.NewGuid());
}
