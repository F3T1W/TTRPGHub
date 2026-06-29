namespace TTRPGHub.Entities;

public readonly record struct GameSessionId(Guid Value)
{
    public static GameSessionId New() => new(Guid.NewGuid());
}
