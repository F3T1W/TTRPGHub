using MediatR;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Campaigns.Queries.GetCampaignDetail;

internal sealed class GetCampaignDetailQueryHandler(
    ICampaignRepository repository,
    IUserRepository userRepository,
    ICurrentUser currentUser
) : IRequestHandler<GetCampaignDetailQuery, Result<CampaignDetailDto>>
{
    public async Task<Result<CampaignDetailDto>> Handle(
        GetCampaignDetailQuery query, CancellationToken ct)
    {
        var campaign = await repository.GetByIdAsync(new CampaignId(query.CampaignId), ct);
        if (campaign is null) return Error.NotFound(nameof(Campaign));

        var organizer = await userRepository.GetByIdAsync(campaign.OrganizerId, ct);

        var participantDtos = new List<CampaignParticipantDto>();
        foreach (var p in campaign.Participants)
        {
            var user = await userRepository.GetByIdAsync(p.UserId, ct);
            participantDtos.Add(new(p.UserId.Value, user?.Username ?? "—", p.Role, p.JoinedAt));
        }

        return new CampaignDetailDto(
            campaign.Id.Value, campaign.Title, campaign.Description, campaign.System, campaign.Status,
            campaign.OrganizerId.Value, organizer?.Username ?? "—",
            participantDtos.AsReadOnly(),
            campaign.OrganizerId == currentUser.Id,
            campaign.Participants.Any(p => p.UserId == currentUser.Id),
            campaign.CreatedAt, campaign.UpdatedAt);
    }
}
