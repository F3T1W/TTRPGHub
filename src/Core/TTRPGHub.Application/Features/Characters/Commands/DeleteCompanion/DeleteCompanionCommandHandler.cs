using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Characters.Commands.DeleteCompanion;

internal sealed class DeleteCompanionCommandHandler(
    ICompanionRepository companionRepository,
    ICharacterRepository characterRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork
) : IRequestHandler<DeleteCompanionCommand, Result>
{
    public async Task<Result> Handle(DeleteCompanionCommand command, CancellationToken ct)
    {
        var companion = await companionRepository.GetByIdAsync(new CompanionId(command.CompanionId), ct);
        if (companion is null)
            return Error.NotFound(nameof(Companion));

        var character = await characterRepository.GetByIdAsync(companion.OwnerCharacterId, ct);
        if (character is null || !character.IsOwnedBy(currentUser.Id))
            return Error.Unauthorized();

        companionRepository.Delete(companion);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
