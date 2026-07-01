using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.GameTable.Shared;

namespace TTRPGHub.Features.GameTable.Commands.RollDice;

public sealed record RollDiceCommand(Guid SessionId, string Expression) : IRequest<Result<TableMessageDto>>;
