using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Pf2e;
using TTRPGHub.Repositories;
using TTRPGHub.Repositories.Pf2e;

namespace TTRPGHub.Features.Characters.Commands.CreateChronicle;

internal sealed class CreateChronicleCommandHandler(
    ICharacterRepository characterRepository,
    IPathfinderSocietyChronicleRepository chronicleRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateChronicleCommand, Result<CreateChronicleResponse>>
{
    public async Task<Result<CreateChronicleResponse>> Handle(CreateChronicleCommand command, CancellationToken ct)
    {
        var character = await characterRepository.GetByIdAsync(new CharacterId(command.CharacterId), ct);
        if (character is null)
            return Error.NotFound(nameof(Character));

        if (!character.IsOwnedBy(currentUser.Id))
            return Error.Unauthorized();

        var chronicleResult = PathfinderSocietyChronicle.Create(
            character.Id, command.ScenarioName, command.SessionDate, command.GmName,
            command.Faction, command.GoldEarned, command.AchievementPoints, command.BoonsUsed, command.Notes);

        if (chronicleResult.IsFailure)
            return chronicleResult.Error!;

        await chronicleRepository.AddAsync(chronicleResult.Value!, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new CreateChronicleResponse(chronicleResult.Value!.Id.Value);
    }
}
