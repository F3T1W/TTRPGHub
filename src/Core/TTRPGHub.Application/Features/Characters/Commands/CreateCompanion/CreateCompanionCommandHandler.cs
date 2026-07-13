using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Characters.Commands.CreateCompanion;

internal sealed class CreateCompanionCommandHandler(
    ICharacterRepository characterRepository,
    ICompanionRepository companionRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateCompanionCommand, Result<CreateCompanionResponse>>
{
    public async Task<Result<CreateCompanionResponse>> Handle(CreateCompanionCommand command, CancellationToken ct)
    {
        var character = await characterRepository.GetByIdAsync(new CharacterId(command.CharacterId), ct);
        if (character is null)
            return Error.NotFound(nameof(Character));

        if (!character.IsOwnedBy(currentUser.Id))
            return Error.Unauthorized();

        var companionResult = Companion.Create(
            character.Id, command.Name, command.Kind, command.Level, command.MaxHitPoints,
            command.ArmorClass, command.Speed, command.AttacksText, command.AbilitiesText, command.Notes);

        if (companionResult.IsFailure)
            return companionResult.Error!;

        await companionRepository.AddAsync(companionResult.Value!, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new CreateCompanionResponse(companionResult.Value!.Id.Value);
    }
}
