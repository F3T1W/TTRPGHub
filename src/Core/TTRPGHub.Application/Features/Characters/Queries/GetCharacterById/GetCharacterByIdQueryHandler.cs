using MediatR;
using TTRPGHub.Application.Common.Interfaces;
using TTRPGHub.Domain.Common;
using TTRPGHub.Domain.Entities;
using TTRPGHub.Domain.Repositories;

namespace TTRPGHub.Application.Features.Characters.Queries.GetCharacterById;

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

        if (character.OwnerId != currentUser.Id && !character.IsPublic)
            return Error.Unauthorized();

        return ToDto(character);
    }

    private static CharacterDto ToDto(Character c) => new(
        c.Id.Value, c.OwnerId.Value, c.Name, c.Race, c.Class,
        c.Level, c.Notes, c.IsPublic, c.CreatedAt, c.UpdatedAt);
}
