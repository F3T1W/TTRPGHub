using System.Text.Json;
using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Characters.Commands.UpdateCharacterFeats;

internal sealed class UpdateCharacterFeatsCommandHandler(
    ICharacterRepository characterRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ICacheService cache
) : IRequestHandler<UpdateCharacterFeatsCommand, Result>
{
    public async Task<Result> Handle(UpdateCharacterFeatsCommand command, CancellationToken ct)
    {
        var character = await characterRepository.GetByIdAsync(new CharacterId(command.CharacterId), ct);
        if (character is null)
            return Error.NotFound(nameof(Character));

        if (character.OwnerId != currentUser.Id)
            return Error.Unauthorized();

        if (!IsValidJson(command.SelectedFeatsJson))
            return Error.Validation("SelectedFeats.Invalid", "Некорректный формат списка фитов.");

        character.SetSelectedFeats(command.SelectedFeatsJson);
        await unitOfWork.SaveChangesAsync(ct);

        await cache.RemoveAsync($"characters:owner:{currentUser.Id}", ct);
        await cache.RemoveAsync($"characters:{command.CharacterId}", ct);

        return Result.Success();
    }

    private static bool IsValidJson(string json)
    {
        try { JsonDocument.Parse(json); return true; }
        catch (JsonException) { return false; }
    }
}
