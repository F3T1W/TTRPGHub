namespace TTRPGHub.Entities;

public readonly record struct CompanionId(Guid Value)
{
    public static CompanionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
