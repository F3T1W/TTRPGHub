using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Characters.Queries.GetCompanions;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Characters.Queries.GetCompanionById;

internal sealed class GetCompanionByIdQueryHandler(
    ICompanionRepository companionRepository,
    ICharacterRepository characterRepository,
    ICurrentUser currentUser
) : IRequestHandler<GetCompanionByIdQuery, Result<CompanionDto>>
{
    public async Task<Result<CompanionDto>> Handle(GetCompanionByIdQuery query, CancellationToken ct)
    {
        var companion = await companionRepository.GetByIdAsync(new CompanionId(query.CompanionId), ct);
        if (companion is null)
            return Error.NotFound(nameof(Companion));

        var character = await characterRepository.GetByIdAsync(companion.OwnerCharacterId, ct);
        if (character is null) return Error.NotFound(nameof(Character));
        if (!character.IsPublic && !character.IsOwnedBy(currentUser.Id))
            return Error.Unauthorized();

        return new CompanionDto(
            companion.Id.Value, companion.Name, companion.Kind, companion.Level,
            companion.MaxHitPoints, companion.CurrentHitPoints, companion.ArmorClass,
            companion.Speed, companion.AttacksText, companion.AbilitiesText, companion.Notes);
    }
}
