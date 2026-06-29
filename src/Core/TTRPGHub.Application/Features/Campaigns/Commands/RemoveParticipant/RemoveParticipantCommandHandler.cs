using MediatR;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Campaigns.Commands.RemoveParticipant;

internal sealed class RemoveParticipantCommandHandler(
    ICampaignRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<RemoveParticipantCommand, Result>
{
    public async Task<Result> Handle(RemoveParticipantCommand command, CancellationToken ct)
    {
        var campaign = await repository.GetByIdAsync(new CampaignId(command.CampaignId), ct);
        if (campaign is null) return Error.NotFound(nameof(Campaign));

        var isOrganizer = campaign.OrganizerId == currentUser.Id;
        var isSelf      = command.UserId == currentUser.Id.Value;
        if (!isOrganizer && !isSelf) return Error.Unauthorized();

        var result = campaign.RemoveParticipant(new UserId(command.UserId));
        if (!result.IsSuccess) return result;

        repository.Update(campaign);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
