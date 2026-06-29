using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Initiative.Commands.CreateTracker;

internal sealed class CreateTrackerCommandHandler(
    IInitiativeTrackerRepository repository,
    ICampaignRepository campaignRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<CreateTrackerCommand, Result<CreateTrackerResponse>>
{
    public async Task<Result<CreateTrackerResponse>> Handle(CreateTrackerCommand command, CancellationToken ct)
    {
        var campaign = await campaignRepository.GetByIdAsync(new CampaignId(command.CampaignId), ct);
        if (campaign is null) return Error.NotFound(nameof(Campaign));
        if (campaign.OrganizerId != currentUser.Id) return Error.Unauthorized();

        var tracker = InitiativeTracker.Create(
            new CampaignId(command.CampaignId), currentUser.Id, command.Name);

        await repository.AddAsync(tracker, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result<CreateTrackerResponse>.Success(new CreateTrackerResponse(tracker.Id.Value));
    }
}
