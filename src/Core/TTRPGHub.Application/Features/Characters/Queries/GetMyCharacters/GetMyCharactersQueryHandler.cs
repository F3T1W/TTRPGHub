using MediatR;
using TTRPGHub.Application.Common.Interfaces;
using TTRPGHub.Domain.Common;
using TTRPGHub.Domain.Entities;
using TTRPGHub.Domain.Repositories;

namespace TTRPGHub.Application.Features.Characters.Queries.GetMyCharacters;

internal sealed class GetMyCharactersQueryHandler(
    ICharacterRepository characterRepository,
    ICurrentUser currentUser
) : IRequestHandler<GetMyCharactersQuery, Result<IReadOnlyList<CharacterSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<CharacterSummaryDto>>> Handle(GetMyCharactersQuery query, CancellationToken ct)
    {
        var characters = await characterRepository.GetByOwnerAsync(currentUser.Id, ct);

        var result = characters
            .Select(ToDto)
            .ToList()
            .AsReadOnly();

        return Result<IReadOnlyList<CharacterSummaryDto>>.Success(result);
    }

    private static CharacterSummaryDto ToDto(Character c) =>
        new(c.Id.Value, c.Name, c.Race, c.Class, c.Level, c.UpdatedAt);
}
