using TTRPGHub.Common;

namespace TTRPGHub.Entities.Moderation;

public sealed class ContentReport : Entity<ContentReportId>
{
    public UserId ReporterId { get; private init; }
    public ReportedEntityType EntityType { get; private init; }
    public Guid EntityId { get; private init; }
    public string Reason { get; private init; } = null!;
    public ReportStatus Status { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime? ResolvedAt { get; private set; }
    public UserId? ResolvedByUserId { get; private set; }

    private ContentReport() { }

    public static ContentReport Create(UserId reporterId, ReportedEntityType entityType, Guid entityId, string reason) =>
        new()
        {
            Id = ContentReportId.New(),
            ReporterId = reporterId,
            EntityType = entityType,
            EntityId = entityId,
            Reason = reason,
            Status = ReportStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

    public void Resolve(UserId moderatorId, ReportStatus status)
    {
        Status = status;
        ResolvedByUserId = moderatorId;
        ResolvedAt = DateTime.UtcNow;
    }
}

public enum ReportStatus { Open, Resolved, Dismissed }
