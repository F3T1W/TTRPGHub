using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.UpdateTokenStats;

public sealed record UpdateTokenStatsCommand(
    Guid SessionId, Guid TokenId, int? CurrentHp, int? Width, int? Height, int? Rotation,
    bool SetInitiative = false, int? Initiative = null,
    bool? HasDarkvision = null,
    bool? HasLowLightVision = null,
    int? CurrentStamina = null,
    int? MaxStamina = null
) : IRequest<Result>;
