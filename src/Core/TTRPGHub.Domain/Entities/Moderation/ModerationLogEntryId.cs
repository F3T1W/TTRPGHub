namespace TTRPGHub.Entities.Moderation;

public readonly record struct ModerationLogEntryId(Guid Value)
{
    public static ModerationLogEntryId New() => new(Guid.NewGuid());
    public static ModerationLogEntryId From(Guid value) => new(value);
}
