using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Events.Queries.GetEventDetail;

public sealed record GetEventDetailQuery(Guid EventId) : IRequest<Result<GameEventDetailDto>>;

public sealed record EventParticipantDto(Guid UserId, string Username, string? AvatarUrl, DateTime RegisteredAt);

public sealed record GameEventDetailDto(
    Guid Id, string Title, string? Description, string System,
    string Format, string? Location, string? OnlineLink,
    DateTime StartsAt, int MaxParticipants, bool IsCancelled,
    Guid OrganizerId, string OrganizerUsername, string? OrganizerAvatarUrl,
    DateTime CreatedAt, List<EventParticipantDto> Participants);
