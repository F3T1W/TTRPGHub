using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.StartCombat;

public sealed record StartCombatCommand(Guid SessionId) : IRequest<Result>;
