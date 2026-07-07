using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable;
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

        var user = await userRepository.GetByIdAsync(currentUser.Id, ct);
        var label = string.IsNullOrWhiteSpace(command.Label) ? null : $"{command.Label}: ";
        string content;

        if (command.Dc is { } dc)
        {
            var check = DiceRoller.RollCheck(command.Expression, dc);
            if (check is null)
                return Error.Validation("Dice.InvalidExpression",
                    "Для проверки со Сложностью нужен ровно один d20 в формуле, например: 1d20+7.");

            content = $"{label}{check.Expression}: {check.Total} ({check.Breakdown}) vs DC {check.Dc} → {DegreeLabel(check.Degree)}";
        }
        else
        {
            var roll = DiceRoller.Roll(command.Expression);
            if (roll is null)
                return Error.Validation("Dice.InvalidExpression", "Не удалось разобрать формулу броска. Пример: 1d20+5.");

            content = $"{label}{roll.Expression}: {roll.Total} ({roll.Breakdown})";
        }

        var message = TableMessage.CreateRoll(session.Id, currentUser.Id, user?.Username ?? "—", content);

        await messageRepository.AddAsync(message, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var dto = new TableMessageDto(message.Id, message.SenderId.Value, message.SenderUsername, null, null, message.Kind, message.Content, message.CreatedAt);
        await notifier.NotifyMessageAsync(command.SessionId, dto, ct);

        return dto;
    }

    private static string DegreeLabel(DegreeOfSuccess degree) => degree switch
    {
        DegreeOfSuccess.CriticalSuccess => "Критический успех!",
        DegreeOfSuccess.Success => "Успех",
        DegreeOfSuccess.Failure => "Провал",
        DegreeOfSuccess.CriticalFailure => "Критический провал!",
        _ => degree.ToString()
    };
}
