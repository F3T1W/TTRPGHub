using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.SetGridCellSize;

public sealed record SetGridCellSizeCommand(Guid SessionId, int Px) : IRequest<Result>;
