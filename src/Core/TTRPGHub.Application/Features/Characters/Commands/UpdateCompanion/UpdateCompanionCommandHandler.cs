using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Characters.Commands.UpdateCompanion;

internal sealed class UpdateCompanionCommandHandler(
    ICompanionRepository companionRepository,
    ICharacterRepository characterRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork
) : IRequestHandler<UpdateCompanionCommand, Result>
{
    public async Task<Result> Handle(UpdateCompanionCommand command, CancellationToken ct)
    {
        var companion = await companionRepository.GetByIdAsync(new CompanionId(command.CompanionId), ct);
        if (companion is null)
            return Error.NotFound(nameof(Companion));

        var character = await characterRepository.GetByIdAsync(companion.OwnerCharacterId, ct);
        if (character is null || character.OwnerId != currentUser.Id)
            return Error.Unauthorized();

        var result = companion.Update(
            command.Name, command.Kind, command.Level, command.MaxHitPoints, command.CurrentHitPoints,
            command.ArmorClass, command.Speed, command.AttacksText, command.AbilitiesText, command.Notes);

        if (result.IsFailure)
            return result;

        companionRepository.Update(companion);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
