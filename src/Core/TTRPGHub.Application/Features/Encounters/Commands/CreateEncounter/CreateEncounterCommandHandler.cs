using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Encounters.Commands.CreateEncounter;

internal sealed class CreateEncounterCommandHandler(
    IEncounterRepository repository,
    ICampaignRepository campaignRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<CreateEncounterCommand, Result<CreateEncounterResponse>>
{
    public async Task<Result<CreateEncounterResponse>> Handle(CreateEncounterCommand command, CancellationToken ct)
    {
        var campaign = await campaignRepository.GetByIdAsync(new CampaignId(command.CampaignId), ct);
        if (campaign is null) return Error.NotFound(nameof(Campaign));

        var isParticipant = campaign.Participants.Any(p => p.UserId == currentUser.Id)
                         || campaign.OrganizerId == currentUser.Id;
        if (!isParticipant) return Error.Unauthorized();

        var encounter = Encounter.Create(
            new CampaignId(command.CampaignId), currentUser.Id,
            command.Title, command.Description, command.Difficulty, command.Notes);

        encounter.SetEntries(command.Entries.Select(e => new EncounterEntry
        {
            Name  = e.Name,
            Count = e.Count,
            Notes = e.Notes,
        }));

        await repository.AddAsync(encounter, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result<CreateEncounterResponse>.Success(new CreateEncounterResponse(encounter.Id.Value));
    }
}
