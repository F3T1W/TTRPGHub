using MediatR;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Campaigns.Commands.CreateCampaign;

internal sealed class CreateCampaignCommandHandler(
    ICampaignRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<CreateCampaignCommand, Result<CreateCampaignResponse>>
{
    public async Task<Result<CreateCampaignResponse>> Handle(
        CreateCampaignCommand command, CancellationToken ct)
    {
        var campaign = Campaign.Create(currentUser.Id, command.Title, command.Description, command.System);
        await repository.AddAsync(campaign, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return new CreateCampaignResponse(campaign.Id.Value, campaign.Title);
    }
}
