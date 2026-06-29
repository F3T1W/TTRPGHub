using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Initiative.Queries.GetTrackerDetail;

internal sealed class GetTrackerDetailQueryHandler(
    IInitiativeTrackerRepository repository,
    ICurrentUser currentUser
) : IRequestHandler<GetTrackerDetailQuery, Result<TrackerDetailDto>>
{
    public async Task<Result<TrackerDetailDto>> Handle(GetTrackerDetailQuery query, CancellationToken ct)
    {
        var tracker = await repository.GetByIdAsync(new InitiativeTrackerId(query.TrackerId), ct);
        if (tracker is null) return Error.NotFound(nameof(InitiativeTracker));

        return Result<TrackerDetailDto>.Success(new TrackerDetailDto(
            tracker.Id.Value, tracker.CampaignId.Value, tracker.OwnerId.Value,
            tracker.Name, tracker.Round, tracker.ActiveEntryIndex, tracker.IsActive,
            tracker.Entries.Select(e => new TrackerEntryDto(
                e.Id, e.Name, e.Initiative, e.MaxHp, e.CurrentHp,
                e.ArmorClass, e.Status, e.IsPlayerCharacter, e.Notes, e.SortOrder)).ToList(),
            tracker.OwnerId == currentUser.Id,
            tracker.UpdatedAt));
    }
}
