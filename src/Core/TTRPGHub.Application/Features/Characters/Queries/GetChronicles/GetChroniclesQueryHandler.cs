using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Pf2e;
using TTRPGHub.Repositories;
using TTRPGHub.Repositories.Pf2e;

namespace TTRPGHub.Features.Characters.Queries.GetChronicles;

internal sealed class GetChroniclesQueryHandler(
    ICharacterRepository characterRepository,
    IPathfinderSocietyChronicleRepository chronicleRepository,
    ICurrentUser currentUser
) : IRequestHandler<GetChroniclesQuery, Result<IReadOnlyList<ChronicleDto>>>
{
    public async Task<Result<IReadOnlyList<ChronicleDto>>> Handle(GetChroniclesQuery query, CancellationToken ct)
    {
        var character = await characterRepository.GetByIdAsync(new CharacterId(query.CharacterId), ct);
        if (character is null)
            return Error.NotFound(nameof(Character));

        if (!character.IsPublic && character.OwnerId != currentUser.Id)
            return Error.Unauthorized();

        var chronicles = await chronicleRepository.GetByCharacterAsync(character.Id, ct);
        return chronicles.Select(ToDto).ToList();
    }

    private static ChronicleDto ToDto(PathfinderSocietyChronicle c) => new(
        c.Id.Value, c.ScenarioName, c.SessionDate, c.GmName, c.Faction,
        c.GoldEarned, c.AchievementPoints, c.BoonsUsed, c.Notes);
}
