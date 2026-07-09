using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Characters.Queries.GetCompanions;

internal sealed class GetCompanionsQueryHandler(
    ICharacterRepository characterRepository,
    ICompanionRepository companionRepository,
    ICurrentUser currentUser
) : IRequestHandler<GetCompanionsQuery, Result<IReadOnlyList<CompanionDto>>>
{
    public async Task<Result<IReadOnlyList<CompanionDto>>> Handle(GetCompanionsQuery query, CancellationToken ct)
    {
        var character = await characterRepository.GetByIdAsync(new CharacterId(query.CharacterId), ct);
        if (character is null)
            return Error.NotFound(nameof(Character));

        if (!character.IsPublic && character.OwnerId != currentUser.Id)
            return Error.Unauthorized();

        var companions = await companionRepository.GetByCharacterAsync(character.Id, ct);
        return companions.Select(ToDto).ToList();
    }

    private static CompanionDto ToDto(Companion c) => new(
        c.Id.Value, c.Name, c.Kind, c.Level, c.MaxHitPoints, c.CurrentHitPoints,
        c.ArmorClass, c.Speed, c.AttacksText, c.AbilitiesText, c.Notes);
}
