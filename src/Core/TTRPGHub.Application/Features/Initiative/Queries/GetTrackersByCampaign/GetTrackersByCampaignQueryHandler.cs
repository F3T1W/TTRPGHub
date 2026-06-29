using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Initiative.Queries.GetTrackersByCampaign;

internal sealed class GetTrackersByCampaignQueryHandler(
    IInitiativeTrackerRepository repository
) : IRequestHandler<GetTrackersByCampaignQuery, Result<IReadOnlyList<TrackerSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<TrackerSummaryDto>>> Handle(
        GetTrackersByCampaignQuery query, CancellationToken ct)
    {
        var trackers = await repository.GetByCampaignAsync(new CampaignId(query.CampaignId), ct);

        IReadOnlyList<TrackerSummaryDto> result = trackers
            .Select(t => new TrackerSummaryDto(
                t.Id.Value, t.CampaignId.Value, t.Name, t.Round,
                t.IsActive, t.Entries.Count, t.UpdatedAt))
            .ToList();

        return Result<IReadOnlyList<TrackerSummaryDto>>.Success(result);
    }
}
