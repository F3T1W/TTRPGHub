using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Pf2e;
using TTRPGHub.Repositories;
using TTRPGHub.Repositories.Pf2e;

namespace TTRPGHub.Features.Characters.Commands.DeleteChronicle;

internal sealed class DeleteChronicleCommandHandler(
    IPathfinderSocietyChronicleRepository chronicleRepository,
    ICharacterRepository characterRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork
) : IRequestHandler<DeleteChronicleCommand, Result>
{
    public async Task<Result> Handle(DeleteChronicleCommand command, CancellationToken ct)
    {
        var chronicle = await chronicleRepository.GetByIdAsync(new PathfinderSocietyChronicleId(command.ChronicleId), ct);
        if (chronicle is null)
            return Error.NotFound(nameof(PathfinderSocietyChronicle));

        var character = await characterRepository.GetByIdAsync(chronicle.CharacterId, ct);
        if (character is null || character.OwnerId != currentUser.Id)
            return Error.Unauthorized();

        chronicleRepository.Delete(chronicle);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
