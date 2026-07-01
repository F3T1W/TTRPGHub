using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Features.Campaigns.Queries.GetMyCampaigns;

namespace TTRPGHub.Features.Campaigns.Queries.GetAllCampaigns;

public sealed record GetAllCampaignsQuery : IRequest<Result<IReadOnlyList<CampaignSummaryDto>>>;
