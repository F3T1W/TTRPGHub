using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Encounters.Commands.UpdateEncounter;

internal sealed class UpdateEncounterCommandHandler(
    IEncounterRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<UpdateEncounterCommand, Result>
{
    public async Task<Result> Handle(UpdateEncounterCommand command, CancellationToken ct)
    {
        var encounter = await repository.GetByIdAsync(new EncounterId(command.EncounterId), ct);
        if (encounter is null) return Error.NotFound(nameof(Encounter));
        if (encounter.CreatedById != currentUser.Id) return Error.Unauthorized();

        encounter.Update(command.Title, command.Description, command.Difficulty, command.Notes);
        encounter.SetEntries(command.Entries.Select(e => new EncounterEntry
        {
            Name  = e.Name,
            Count = e.Count,
            Notes = e.Notes,
        }));

        repository.Update(encounter);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
