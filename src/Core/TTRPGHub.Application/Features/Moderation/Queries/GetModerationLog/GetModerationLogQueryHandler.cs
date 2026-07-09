using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Moderation.Queries.GetModerationLog;

internal sealed class GetModerationLogQueryHandler(
    IModerationLogRepository moderationLog,
    IUserRepository users
) : IRequestHandler<GetModerationLogQuery, Result<List<ModerationLogEntryDto>>>
{
    public async Task<Result<List<ModerationLogEntryDto>>> Handle(GetModerationLogQuery request, CancellationToken ct)
    {
        var entries = await moderationLog.GetRecentAsync(200, ct);
        var result = new List<ModerationLogEntryDto>(entries.Count);

        foreach (var e in entries)
        {
            var actor = await users.GetByIdAsync(e.ActorUserId, ct);
            result.Add(new ModerationLogEntryDto(
                e.Id.Value, e.ActorUserId.Value, actor?.Username ?? "—", e.Action,
                e.TargetType, e.TargetId, e.CreatedAt, e.Details));
        }

        return result;
    }
}
