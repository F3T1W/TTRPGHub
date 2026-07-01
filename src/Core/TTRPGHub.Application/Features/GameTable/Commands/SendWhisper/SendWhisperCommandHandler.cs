using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.SendWhisper;

internal sealed class SendWhisperCommandHandler(
    IGameSessionRepository sessionRepository,
    ITableMessageRepository messageRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<SendWhisperCommand, Result<TableMessageDto>>
{
    public async Task<Result<TableMessageDto>> Handle(SendWhisperCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Content) || command.Content.Length > 1000)
            return Error.Validation("TableMessage.Invalid", "Сообщение пустое или слишком длинное.");

        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        // Только ГМ может шептать — обращение игрока к игроку или к ГМ не поддерживается в этой фазе
        if (session.OrganizerId != currentUser.Id)
            return Error.Unauthorized();

        var recipientId = new UserId(command.RecipientUserId);
        if (!session.IsParticipant(recipientId) || recipientId == currentUser.Id)
            return Error.Validation("Whisper.InvalidRecipient", "Получатель должен быть участником сессии.");

        var sender = await userRepository.GetByIdAsync(currentUser.Id, ct);
        var recipient = await userRepository.GetByIdAsync(recipientId, ct);

        var message = TableMessage.CreateWhisper(
            session.Id, currentUser.Id, sender?.Username ?? "—",
            recipientId, recipient?.Username ?? "—", command.Content.Trim());

        await messageRepository.AddAsync(message, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var dto = new TableMessageDto(
            message.Id, message.SenderId.Value, message.SenderUsername,
            message.RecipientId!.Value.Value, message.RecipientUsername,
            message.Kind, message.Content, message.CreatedAt);

        await notifier.NotifyWhisperAsync(currentUser.Id.Value, command.RecipientUserId, dto, ct);

        return dto;
    }
}
