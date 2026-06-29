namespace TTRPGHub.Entities;

public readonly record struct InitiativeTrackerId(Guid Value)
{
    public static InitiativeTrackerId New() => new(Guid.NewGuid());
    public static InitiativeTrackerId Empty => new(Guid.Empty);
}
