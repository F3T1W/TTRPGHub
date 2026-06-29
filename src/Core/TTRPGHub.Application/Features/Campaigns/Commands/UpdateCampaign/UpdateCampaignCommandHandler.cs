using MediatR;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Campaigns.Commands.UpdateCampaign;

internal sealed class UpdateCampaignCommandHandler(
    ICampaignRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<UpdateCampaignCommand, Result>
{
    public async Task<Result> Handle(UpdateCampaignCommand command, CancellationToken ct)
    {
        var campaign = await repository.GetByIdAsync(new CampaignId(command.CampaignId), ct);
        if (campaign is null) return Error.NotFound(nameof(Campaign));
        if (campaign.OrganizerId != currentUser.Id) return Error.Unauthorized();

        campaign.Update(command.Title, command.Description, command.System);
        repository.Update(campaign);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
