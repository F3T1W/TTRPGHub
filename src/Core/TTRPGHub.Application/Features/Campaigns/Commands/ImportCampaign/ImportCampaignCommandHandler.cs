using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Campaigns.Commands.ImportCampaign;

internal sealed class ImportCampaignCommandHandler(
    ICampaignRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<ImportCampaignCommand, Result<ImportCampaignResponse>>
{
    public async Task<Result<ImportCampaignResponse>> Handle(ImportCampaignCommand cmd, CancellationToken ct)
    {
        var campaign = Campaign.Create(currentUser.Id, cmd.Title, cmd.Description, cmd.System);
        await repository.AddAsync(campaign, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return new ImportCampaignResponse(campaign.Id.Value, campaign.Title);
    }
}
