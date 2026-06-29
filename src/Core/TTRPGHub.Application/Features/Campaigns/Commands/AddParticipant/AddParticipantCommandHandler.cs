using MediatR;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Campaigns.Commands.AddParticipant;

internal sealed class AddParticipantCommandHandler(
    ICampaignRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<AddParticipantCommand, Result>
{
    public async Task<Result> Handle(AddParticipantCommand command, CancellationToken ct)
    {
        var campaign = await repository.GetByIdAsync(new CampaignId(command.CampaignId), ct);
        if (campaign is null) return Error.NotFound(nameof(Campaign));
        if (campaign.OrganizerId != currentUser.Id) return Error.Unauthorized();

        var result = campaign.AddParticipant(new UserId(command.UserId));
        if (!result.IsSuccess) return result;

        repository.Update(campaign);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
