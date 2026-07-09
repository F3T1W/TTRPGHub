namespace TTRPGHub.Entities;

public readonly record struct TicketCommentId(Guid Value)
{
    public static TicketCommentId New() => new(Guid.NewGuid());
    public static TicketCommentId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
