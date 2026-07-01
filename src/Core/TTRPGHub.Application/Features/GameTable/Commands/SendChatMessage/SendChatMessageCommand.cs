using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.GameTable.Shared;

namespace TTRPGHub.Features.GameTable.Commands.SendChatMessage;

public sealed record SendChatMessageCommand(Guid SessionId, string Content) : IRequest<Result<TableMessageDto>>;
