namespace TTRPGHub.Entities;

public readonly record struct CharacterId(Guid Value)
{
    public static CharacterId New() => new(Guid.NewGuid());
    public static CharacterId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
