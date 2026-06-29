using TTRPGHub.Common;

namespace TTRPGHub.Entities;

public sealed class SessionNote : Entity<SessionNoteId>
{
    public new SessionNoteId Id { get; private set; }
    public CampaignId CampaignId { get; private set; }
    public UserId AuthorId { get; private set; }
    public string Title { get; private set; } = "";
    public string Content { get; private set; } = "";
    public DateTime SessionDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private SessionNote() { }

    public static SessionNote Create(CampaignId campaignId, UserId authorId, string title, string content, DateTime sessionDate)
    {
        return new SessionNote
        {
            Id          = SessionNoteId.New(),
            CampaignId  = campaignId,
            AuthorId    = authorId,
            Title       = title,
            Content     = content,
            SessionDate = sessionDate,
            CreatedAt   = DateTime.UtcNow,
            UpdatedAt   = DateTime.UtcNow,
        };
    }

    public Result Update(string title, string content, DateTime sessionDate)
    {
        Title       = title;
        Content     = content;
        SessionDate = sessionDate;
        UpdatedAt   = DateTime.UtcNow;
        return Result.Success();
    }
}
