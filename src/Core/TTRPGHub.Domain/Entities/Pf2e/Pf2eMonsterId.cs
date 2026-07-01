namespace TTRPGHub.Entities.Pf2e;

public readonly record struct Pf2eMonsterId(Guid Value)
{
    public static Pf2eMonsterId New() => new(Guid.NewGuid());
}
