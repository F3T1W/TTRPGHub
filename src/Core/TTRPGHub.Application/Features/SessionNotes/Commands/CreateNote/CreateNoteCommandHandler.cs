using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.SessionNotes.Commands.CreateNote;

internal sealed class CreateNoteCommandHandler(
    ISessionNoteRepository noteRepository,
    ICampaignRepository campaignRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<CreateNoteCommand, Result<CreateNoteResponse>>
{
    public async Task<Result<CreateNoteResponse>> Handle(CreateNoteCommand command, CancellationToken ct)
    {
        var campaign = await campaignRepository.GetByIdAsync(new CampaignId(command.CampaignId), ct);
        if (campaign is null) return Error.NotFound(nameof(Campaign));

        var isParticipant = campaign.Participants.Any(p => p.UserId == currentUser.Id)
                         || campaign.OrganizerId == currentUser.Id;
        if (!isParticipant) return Error.Unauthorized();

        var note = SessionNote.Create(
            new CampaignId(command.CampaignId), currentUser.Id,
            command.Title, command.Content, command.SessionDate);

        await noteRepository.AddAsync(note, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result<CreateNoteResponse>.Success(new CreateNoteResponse(note.Id.Value));
    }
}
