using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Events.Queries.GetEvents;

public sealed record GetEventsQuery(int Page, int PageSize) : IRequest<Result<EventsPagedResult>>;

public sealed record GameEventSummaryDto(
    Guid Id, string Title, string System, string Format,
    string? Location, string? OnlineLink, DateTime StartsAt,
    int MaxParticipants, int ParticipantCount,
    Guid OrganizerId, string OrganizerUsername, bool IsCancelled);

public sealed record EventsPagedResult(List<GameEventSummaryDto> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}
