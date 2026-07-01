namespace TTRPGHub.Entities;

public readonly record struct SupportTicketId(Guid Value)
{
    public static SupportTicketId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
