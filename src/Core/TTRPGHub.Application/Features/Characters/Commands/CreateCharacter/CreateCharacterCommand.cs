using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Characters.Commands.CreateCharacter;

public sealed record CreateCharacterCommand(
    string Name,
    string Race,
    string Class,
    int Level
) : IRequest<Result<CreateCharacterResponse>>;

public sealed record CreateCharacterResponse(Guid CharacterId, string Name, string Race, string Class, int Level);
