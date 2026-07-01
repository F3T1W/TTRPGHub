using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.GameTable.Shared;

namespace TTRPGHub.Features.GameTable.Commands.AddTableToken;

public sealed record AddTableTokenCommand(
    Guid SessionId, string Label, string? ImageUrl, string Color,
    double X, double Y, Guid? OwnerUserId
) : IRequest<Result<TableTokenDto>>;
