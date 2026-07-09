using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Moderation.Queries.GetModerationLog;

public sealed record GetModerationLogQuery : IRequest<Result<List<ModerationLogEntryDto>>>;

public sealed record ModerationLogEntryDto(
    Guid Id, Guid ActorUserId, string ActorUsername, string Action,
    string TargetType, Guid TargetId, DateTime CreatedAt, string? Details);
