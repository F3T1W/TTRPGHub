using TTRPGHub.Common;

namespace TTRPGHub.Entities;

public sealed class TicketComment : Entity<TicketCommentId>
{
    public SupportTicketId TicketId { get; private init; }
    public UserId AuthorId { get; private init; }
    public string Body { get; private init; } = null!;
    public DateTime CreatedAt { get; private init; }

    private TicketComment() { }

    public static TicketComment Create(SupportTicketId ticketId, UserId authorId, string body) =>
        new()
        {
            Id = TicketCommentId.New(),
            TicketId = ticketId,
            AuthorId = authorId,
            Body = body,
            CreatedAt = DateTime.UtcNow
        };
}
