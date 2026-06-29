namespace TTRPGHub.Entities.Dnd5e;

public readonly record struct Dnd5eMonsterId(Guid Value)
{
    public static Dnd5eMonsterId New() => new(Guid.NewGuid());
}
