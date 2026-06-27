using MediatR;
using TTRPGHub.Domain.Common;

namespace TTRPGHub.Application.Features.Characters.Queries.GetCharacterById;

public sealed record GetCharacterByIdQuery(Guid CharacterId) : IRequest<Result<CharacterDto>>;

public sealed record CharacterDto(
    Guid Id,
    Guid OwnerId,
    string Name,
    string Race,
    string Class,
    int Level,
    string? Notes,
    bool IsPublic,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
