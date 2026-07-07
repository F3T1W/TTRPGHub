using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.AdvanceTurn;

public sealed record AdvanceTurnCommand(Guid SessionId, bool Forward) : IRequest<Result>;
