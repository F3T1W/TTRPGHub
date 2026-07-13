using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Characters.Queries.GetCharacterById;

internal sealed class GetCharacterByIdQueryHandler(
    ICharacterRepository characterRepository,
    ICurrentUser currentUser
) : IRequestHandler<GetCharacterByIdQuery, Result<CharacterDto>>
{
    public async Task<Result<CharacterDto>> Handle(GetCharacterByIdQuery query, CancellationToken ct)
    {
        var character = await characterRepository.GetByIdAsync(new CharacterId(query.CharacterId), ct);

        if (character is null)
            return Error.NotFound(nameof(Character));

        if (!character.IsPublic && !character.IsOwnedBy(currentUser.Id))
            return Error.Unauthorized();

        return ToDto(character);
    }

    private static CharacterDto ToDto(Character c) => new(
        c.Id.Value, c.OwnerId.Value, c.Name, c.Race, c.Class,
        c.Level, c.IsPublic, c.CreatedAt, c.UpdatedAt);
}
