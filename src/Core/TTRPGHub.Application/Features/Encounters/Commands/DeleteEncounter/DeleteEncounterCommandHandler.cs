using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Encounters.Commands.DeleteEncounter;

internal sealed class DeleteEncounterCommandHandler(
    IEncounterRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<DeleteEncounterCommand, Result>
{
    public async Task<Result> Handle(DeleteEncounterCommand command, CancellationToken ct)
    {
        var encounter = await repository.GetByIdAsync(new EncounterId(command.EncounterId), ct);
        if (encounter is null) return Error.NotFound(nameof(Encounter));
        if (encounter.CreatedById != currentUser.Id) return Error.Unauthorized();

        repository.Delete(encounter);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
