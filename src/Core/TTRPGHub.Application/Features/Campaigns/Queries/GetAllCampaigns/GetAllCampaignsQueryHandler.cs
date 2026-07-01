using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Features.Campaigns.Queries.GetMyCampaigns;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Campaigns.Queries.GetAllCampaigns;

internal sealed class GetAllCampaignsQueryHandler(
    ICampaignRepository repository
) : IRequestHandler<GetAllCampaignsQuery, Result<IReadOnlyList<CampaignSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<CampaignSummaryDto>>> Handle(
        GetAllCampaignsQuery query, CancellationToken ct)
    {
        var campaigns = await repository.GetActiveAsync(ct);
        var result = campaigns
            .Select(c => new CampaignSummaryDto(
                c.Id.Value, c.Title, c.Description, c.System, c.Status,
                c.Participants.Count, false,
                c.CreatedAt, c.UpdatedAt))
            .ToList()
            .AsReadOnly();
        return Result<IReadOnlyList<CampaignSummaryDto>>.Success(result);
    }
}
