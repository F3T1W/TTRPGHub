using System.Text.Json;
using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Characters.Commands.UpdateCharacterPf2eStats;

internal sealed class UpdateCharacterPf2eStatsCommandHandler(
    ICharacterRepository characterRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ICacheService cache
) : IRequestHandler<UpdateCharacterPf2eStatsCommand, Result>
{
    public async Task<Result> Handle(UpdateCharacterPf2eStatsCommand command, CancellationToken ct)
    {
        var character = await characterRepository.GetByIdAsync(new CharacterId(command.CharacterId), ct);
        if (character is null)
            return Error.NotFound(nameof(Character));

        if (character.OwnerId != currentUser.Id)
            return Error.Unauthorized();

        if (!IsValidJson(command.StatsJson))
            return Error.Validation("Pf2eStats.Invalid", "Некорректный формат PF2e-статов.");

        character.SetPf2eStats(command.StatsJson);
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
