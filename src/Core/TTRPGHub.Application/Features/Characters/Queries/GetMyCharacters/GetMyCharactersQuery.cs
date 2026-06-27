using MediatR;
using TTRPGHub.Application.Common.Interfaces;
using TTRPGHub.Domain.Common;

namespace TTRPGHub.Application.Features.Characters.Queries.GetMyCharacters;

public sealed record GetMyCharactersQuery : IRequest<Result<IReadOnlyList<CharacterSummaryDto>>>, ICacheableQuery
{
    public string CacheKey => $"characters:owner:{OwnerId}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
    public Guid OwnerId { get; init; }
}

public sealed record CharacterSummaryDto(
    Guid Id,
    string Name,
    string Race,
    string Class,
    int Level,
    DateTime UpdatedAt
);
