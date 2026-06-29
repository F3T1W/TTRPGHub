using MediatR;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Campaigns.Commands.ChangeCampaignStatus;

internal sealed class ChangeCampaignStatusCommandHandler(
    ICampaignRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<ChangeCampaignStatusCommand, Result>
{
    public async Task<Result> Handle(ChangeCampaignStatusCommand command, CancellationToken ct)
    {
        var campaign = await repository.GetByIdAsync(new CampaignId(command.CampaignId), ct);
        if (campaign is null) return Error.NotFound(nameof(Campaign));
        if (campaign.OrganizerId != currentUser.Id) return Error.Unauthorized();

        campaign.ChangeStatus(command.Status);
        repository.Update(campaign);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
