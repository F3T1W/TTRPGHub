using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Characters.Commands.LevelUpCharacter;

public sealed record LevelUpCharacterCommand(Guid CharacterId, int NewLevel) : IRequest<Result<LevelUpResponse>>;

public sealed record LevelUpResponse(Guid CharacterId, int Level, int MaxHitPoints, int CurrentHitPoints, string? WhatsNew);
