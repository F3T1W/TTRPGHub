using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Features.Campaigns.Queries.GetMyCampaigns;

public sealed record GetMyCampaignsQuery : IRequest<Result<IReadOnlyList<CampaignSummaryDto>>>;

public sealed record CampaignSummaryDto(
    Guid Id,
    string Title,
    string? Description,
    string System,
    CampaignStatus Status,
    int ParticipantCount,
    bool IsOrganizer,
    DateTime CreatedAt,
    DateTime UpdatedAt);
