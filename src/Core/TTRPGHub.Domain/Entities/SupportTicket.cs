using TTRPGHub.Common;

namespace TTRPGHub.Entities;

public sealed class SupportTicket : Entity<SupportTicketId>
{
    private readonly List<TicketAttachment> _attachments = [];

    public UserId ReporterId { get; private init; }
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string? ContactInfo { get; private init; }
    public TicketStatus Status { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime UpdatedAt { get; private set; }
    public IReadOnlyList<TicketAttachment> Attachments => _attachments.AsReadOnly();

    private SupportTicket() { }

    public static SupportTicket Create(UserId reporterId, string title, string description, string? contactInfo)
    {
        var now = DateTime.UtcNow;
        return new SupportTicket
        {
            Id = SupportTicketId.New(),
            ReporterId = reporterId,
            Title = title,
            Description = description,
            ContactInfo = contactInfo,
            Status = TicketStatus.Open,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void AddAttachment(string url, string fileName, string contentType)
    {
        _attachments.Add(new TicketAttachment(Guid.NewGuid(), url, fileName, contentType));
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(TicketStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }
}

public sealed record TicketAttachment(Guid Id, string Url, string FileName, string ContentType);

public enum TicketStatus { Open, InProgress, Done }
