using TTRPGHub.Common;

namespace TTRPGHub.Entities.Moderation;

public sealed class ModerationLogEntry : Entity<ModerationLogEntryId>
{
    public UserId ActorUserId { get; private init; }
    public string Action { get; private init; } = null!;
    public string TargetType { get; private init; } = null!;
    public Guid TargetId { get; private init; }
    public DateTime CreatedAt { get; private init; }
    public string? Details { get; private init; }

    private ModerationLogEntry() { }

    public static ModerationLogEntry Create(UserId actorUserId, string action, string targetType, Guid targetId, string? details = null) =>
        new()
        {
            Id = ModerationLogEntryId.New(),
            ActorUserId = actorUserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            CreatedAt = DateTime.UtcNow,
            Details = details
        };
}
