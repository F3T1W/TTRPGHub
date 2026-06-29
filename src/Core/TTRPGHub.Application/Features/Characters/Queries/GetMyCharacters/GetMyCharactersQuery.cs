using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Characters.Queries.GetMyCharacters;

public sealed record GetMyCharactersQuery : IRequest<Result<IReadOnlyList<CharacterSummaryDto>>>;

public sealed record CharacterSummaryDto(
    Guid Id,
    string Name,
    string Race,
    string Class,
    int Level,
    string? AvatarUrl,
    DateTime UpdatedAt
);
