using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Characters.Queries.GetCharacterById;

public sealed record GetCharacterByIdQuery(Guid CharacterId) : IRequest<Result<CharacterDto>>;

public sealed record CharacterDto(
    Guid Id,
    Guid OwnerId,
    string Name,
    string Race,
    string Class,
    int Level,
    bool IsPublic,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
