using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Features.Sessions.Queries.GetUpcomingSessions;

public sealed record GetUpcomingSessionsQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<IReadOnlyList<SessionSummaryDto>>>;

public sealed record SessionSummaryDto(
    Guid Id,
    string Title,
    string? Description,
    string System,
    int MaxPlayers,
    int CurrentPlayers,
    DateTime ScheduledAt,
    SessionStatus Status,
    Guid OrganizerId,
    string OrganizerName
);
