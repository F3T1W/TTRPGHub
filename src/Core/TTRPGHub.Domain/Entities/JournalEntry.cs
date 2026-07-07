namespace TTRPGHub.Entities;

// Заметки мастера, привязанные к сессии — аналог Foundry Journal. L.7: per-player visibility
// (VisibleToUserIdsJson), папки (ParentId), опциональная привязка к кампании (CampaignId).
public sealed class JournalEntry
{
    public Guid Id { get; private init; }
    public GameSessionId SessionId { get; private init; }
    public CampaignId? CampaignId { get; private set; }
    public Guid? ParentId { get; private set; }
    public UserId AuthorId { get; private init; }
    public string Title { get; private set; } = null!;
    public string ContentMarkdown { get; private set; } = null!;
    public bool IsPublished { get; private set; }
    public string? VisibleToUserIdsJson { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime UpdatedAt { get; private set; }

    private JournalEntry() { }

    public static JournalEntry Create(
        GameSessionId sessionId, UserId authorId, string title, string contentMarkdown,
        Guid? parentId = null, CampaignId? campaignId = null)
    {
        var now = DateTime.UtcNow;
        return new JournalEntry
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            CampaignId = campaignId,
            ParentId = parentId,
            AuthorId = authorId,
            Title = title,
            ContentMarkdown = contentMarkdown,
            IsPublished = false,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string title, string contentMarkdown, Guid? parentId = null, CampaignId? campaignId = null)
    {
        Title = title;
        ContentMarkdown = contentMarkdown;
        ParentId = parentId;
        CampaignId = campaignId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetVisibility(string? visibleToUserIdsJson)
    {
        VisibleToUserIdsJson = visibleToUserIdsJson;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPublished(bool published)
    {
        IsPublished = published;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsVisibleTo(bool isOrganizer, UserId userId, IReadOnlyCollection<Guid>? visibleToUserIds)
    {
        if (isOrganizer) return true;
        if (!IsPublished) return false;
        return visibleToUserIds is null || visibleToUserIds.Contains(userId.Value);
    }
}
