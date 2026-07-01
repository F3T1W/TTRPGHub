using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.GameTable.Shared;

namespace TTRPGHub.Features.GameTable.Commands.SendWhisper;

public sealed record SendWhisperCommand(Guid SessionId, Guid RecipientUserId, string Content) : IRequest<Result<TableMessageDto>>;
