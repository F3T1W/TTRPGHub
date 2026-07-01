namespace TTRPGHub.Entities;

public readonly record struct RuleEntryId(Guid Value)
{
    public static RuleEntryId New() => new(Guid.NewGuid());
}
