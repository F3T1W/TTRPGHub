using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Initiative.Queries.GetTrackersByCampaign;

public sealed record GetTrackersByCampaignQuery(Guid CampaignId)
    : IRequest<Result<IReadOnlyList<TrackerSummaryDto>>>;

public sealed record TrackerSummaryDto(
    Guid Id, Guid CampaignId, string Name, int Round, bool IsActive,
    int EntryCount, DateTime UpdatedAt);
