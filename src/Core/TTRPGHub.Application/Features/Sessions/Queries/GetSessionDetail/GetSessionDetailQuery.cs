using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Features.Sessions.Queries.GetSessionDetail;

public sealed record GetSessionDetailQuery(Guid SessionId) : IRequest<Result<SessionDetailDto>>;

public sealed record SessionDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string System,
    int MaxPlayers,
    DateTime ScheduledAt,
    SessionFormat Format,
    string? Location,
    SessionStatus Status,
    Guid OrganizerId,
    string OrganizerName,
    IReadOnlyList<SessionParticipantDto> Participants,
    bool IsCurrentUserParticipant,
    bool IsCurrentUserOrganizer
);

public sealed record SessionParticipantDto(Guid UserId, string Username, ParticipantRole Role, DateTime JoinedAt);
