using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Events.Queries.GetEvents;

internal sealed class GetEventsQueryHandler(IGameEventRepository repo)
    : IRequestHandler<GetEventsQuery, Result<EventsPagedResult>>
{
    public async Task<Result<EventsPagedResult>> Handle(GetEventsQuery request, CancellationToken ct)
    {
        var items = await repo.GetUpcomingAsync(request.Page, request.PageSize, ct);
        var total = await repo.CountUpcomingAsync(ct);

        var dtos = items.Select(e => new GameEventSummaryDto(
            e.Id.Value, e.Title, e.System, e.Format.ToString(),
            e.Location, e.OnlineLink, e.StartsAt,
            e.MaxParticipants, e.Participants.Count,
            e.OrganizerId.Value, e.Organizer?.Username ?? "?",
            e.IsCancelled)).ToList();

        return Result<EventsPagedResult>.Success(
            new EventsPagedResult(dtos, total, request.Page, request.PageSize));
    }
}
