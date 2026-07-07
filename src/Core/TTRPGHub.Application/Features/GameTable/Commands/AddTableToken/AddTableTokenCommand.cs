using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.GameTable.Shared;

namespace TTRPGHub.Features.GameTable.Commands.AddTableToken;

public sealed record AddTableTokenCommand(
    Guid SessionId, string Label, string? ImageUrl, string Color,
    double X, double Y, Guid? OwnerUserId,
    int Width = 1, int Height = 1,
    string CombatantType = "None", Guid? CombatantId = null
) : IRequest<Result<TableTokenDto>>;
