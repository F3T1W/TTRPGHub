using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Characters.Commands.UpdateCharacterPf2eStats;

public sealed record UpdateCharacterPf2eStatsCommand(Guid CharacterId, string StatsJson) : IRequest<Result>;
