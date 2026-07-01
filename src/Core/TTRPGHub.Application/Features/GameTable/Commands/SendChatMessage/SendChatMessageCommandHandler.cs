using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.SendChatMessage;

internal sealed class SendChatMessageCommandHandler(
    IGameSessionRepository sessionRepository,
    ITableMessageRepository messageRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<SendChatMessageCommand, Result<TableMessageDto>>
{
    public async Task<Result<TableMessageDto>> Handle(SendChatMessageCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Content) || command.Content.Length > 1000)
            return Error.Validation("TableMessage.Invalid", "Сообщение пустое или слишком длинное.");

        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        if (!session.IsParticipant(currentUser.Id))
            return Error.Unauthorized();

        var user = await userRepository.GetByIdAsync(currentUser.Id, ct);
        var message = TableMessage.CreateChat(session.Id, currentUser.Id, user?.Username ?? "—", command.Content.Trim());

        await messageRepository.AddAsync(message, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var dto = new TableMessageDto(message.Id, message.SenderId.Value, message.SenderUsername, null, null, message.Kind, message.Content, message.CreatedAt);
        await notifier.NotifyMessageAsync(command.SessionId, dto, ct);

        return dto;
    }
}
