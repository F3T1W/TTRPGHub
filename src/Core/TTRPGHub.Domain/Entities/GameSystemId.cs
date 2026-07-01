namespace TTRPGHub.Entities;

public readonly record struct GameSystemId(Guid Value)
{
    public static GameSystemId New() => new(Guid.NewGuid());
}
