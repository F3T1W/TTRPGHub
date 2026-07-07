using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.EndCombat;

public sealed record EndCombatCommand(Guid SessionId) : IRequest<Result>;
