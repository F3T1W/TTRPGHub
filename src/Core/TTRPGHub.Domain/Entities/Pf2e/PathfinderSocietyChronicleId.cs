namespace TTRPGHub.Entities.Pf2e;

public readonly record struct PathfinderSocietyChronicleId(Guid Value)
{
    public static PathfinderSocietyChronicleId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
