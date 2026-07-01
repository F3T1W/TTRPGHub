using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.RollDice;

internal sealed class RollDiceCommandHandler(
    IGameSessionRepository sessionRepository,
    ITableMessageRepository messageRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<RollDiceCommand, Result<TableMessageDto>>
{
    public async Task<Result<TableMessageDto>> Handle(RollDiceCommand command, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        if (!session.IsParticipant(currentUser.Id))
            return Error.Unauthorized();

        var roll = DiceRoller.Roll(command.Expression);
        if (roll is null)
            return Error.Validation("Dice.InvalidExpression", "Не удалось разобрать формулу броска. Пример: 1d20+5.");

        var user = await userRepository.GetByIdAsync(currentUser.Id, ct);
        var content = $"{roll.Expression}: {roll.Total} ({roll.Breakdown})";
        var message = TableMessage.CreateRoll(session.Id, currentUser.Id, user?.Username ?? "—", content);

        await messageRepository.AddAsync(message, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var dto = new TableMessageDto(message.Id, message.SenderId.Value, message.SenderUsername, null, null, message.Kind, message.Content, message.CreatedAt);
        await notifier.NotifyMessageAsync(command.SessionId, dto, ct);

        return dto;
    }
}
