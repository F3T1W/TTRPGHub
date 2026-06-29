using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Characters.Queries.GetMyCharacters;

internal sealed class GetMyCharactersQueryHandler(
    ICharacterRepository characterRepository,
    ICurrentUser currentUser,
    ICacheService cache
) : IRequestHandler<GetMyCharactersQuery, Result<IReadOnlyList<CharacterSummaryDto>>>
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public async Task<Result<IReadOnlyList<CharacterSummaryDto>>> Handle(GetMyCharactersQuery query, CancellationToken ct)
    {
        var cacheKey = $"characters:owner:{currentUser.Id.Value}";

        var cached = await cache.GetAsync<IReadOnlyList<CharacterSummaryDto>>(cacheKey, ct);
        if (cached is not null)
            return Result<IReadOnlyList<CharacterSummaryDto>>.Success(cached);

        var characters = await characterRepository.GetByOwnerAsync(currentUser.Id, ct);
        var result = characters.Select(ToDto).ToList().AsReadOnly();

        await cache.SetAsync(cacheKey, result, Ttl, ct);
        return Result<IReadOnlyList<CharacterSummaryDto>>.Success(result);
    }

    private static CharacterSummaryDto ToDto(Character c) =>
        new(c.Id.Value, c.Name, c.Race, c.Class, c.Level, c.AvatarUrl, c.UpdatedAt);
}
