using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.SetWalls;

public sealed record SetWallsCommand(Guid SessionId, string? WallsJson) : IRequest<Result>;
