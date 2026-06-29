namespace TTRPGHub.Entities.Events;

public readonly record struct GameEventId(Guid Value)
{
    public static GameEventId New() => new(Guid.NewGuid());
    public static GameEventId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
