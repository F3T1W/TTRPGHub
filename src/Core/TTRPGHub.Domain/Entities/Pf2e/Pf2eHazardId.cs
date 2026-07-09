namespace TTRPGHub.Entities.Pf2e;

public readonly record struct Pf2eHazardId(Guid Value)
{
    public static Pf2eHazardId New() => new(Guid.NewGuid());
}
