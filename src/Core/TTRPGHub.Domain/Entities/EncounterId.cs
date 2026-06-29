namespace TTRPGHub.Entities;

public readonly record struct EncounterId(Guid Value)
{
    public static EncounterId New() => new(Guid.NewGuid());
    public static EncounterId Empty => new(Guid.Empty);
}
