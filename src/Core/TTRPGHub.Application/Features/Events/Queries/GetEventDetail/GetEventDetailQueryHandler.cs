using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities.Events;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Events.Queries.GetEventDetail;

internal sealed class GetEventDetailQueryHandler(IGameEventRepository repo)
    : IRequestHandler<GetEventDetailQuery, Result<GameEventDetailDto>>
{
    public async Task<Result<GameEventDetailDto>> Handle(GetEventDetailQuery request, CancellationToken ct)
    {
        var e = await repo.GetByIdWithParticipantsAsync(GameEventId.From(request.EventId), ct);
        if (e is null) return Error.NotFound(nameof(GameEvent));

        var participants = e.Participants.Select(p => new EventParticipantDto(
            p.UserId.Value,
            p.User?.Username ?? "?",
            p.User?.Profile.AvatarUrl,
            p.RegisteredAt)).ToList();

        return Result<GameEventDetailDto>.Success(new GameEventDetailDto(
            e.Id.Value, e.Title, e.Description, e.System,
            e.Format.ToString(), e.Location, e.OnlineLink,
            e.StartsAt, e.MaxParticipants, e.IsCancelled,
            e.OrganizerId.Value, e.Organizer?.Username ?? "?", e.Organizer?.Profile.AvatarUrl,
            e.CreatedAt, participants));
    }
}
