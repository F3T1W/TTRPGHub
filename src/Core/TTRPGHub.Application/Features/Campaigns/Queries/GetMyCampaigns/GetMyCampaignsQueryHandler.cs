using MediatR;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Campaigns.Queries.GetMyCampaigns;

internal sealed class GetMyCampaignsQueryHandler(
    ICampaignRepository repository,
    ICurrentUser currentUser
) : IRequestHandler<GetMyCampaignsQuery, Result<IReadOnlyList<CampaignSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<CampaignSummaryDto>>> Handle(
        GetMyCampaignsQuery query, CancellationToken ct)
    {
        var campaigns = await repository.GetByParticipantAsync(currentUser.Id, ct);
        var result = campaigns.Select(c => ToDto(c, currentUser.Id)).ToList().AsReadOnly();
        return Result<IReadOnlyList<CampaignSummaryDto>>.Success(result);
    }

    private static CampaignSummaryDto ToDto(Campaign c, UserId currentUserId) => new(
        c.Id.Value, c.Title, c.Description, c.System, c.Status,
        c.Participants.Count, c.OrganizerId == currentUserId,
        c.CreatedAt, c.UpdatedAt);
}
