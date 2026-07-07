using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.ApplyTokenCondition;

internal sealed class ApplyTokenConditionCommandHandler(
    IGameSessionRepository sessionRepository,
    ITableTokenRepository tokenRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<ApplyTokenConditionCommand, Result>
{
    public async Task<Result> Handle(ApplyTokenConditionCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Slug) || string.IsNullOrWhiteSpace(command.Name))
            return Error.Validation("TokenCondition.Invalid", "Название состояния обязательно.");

        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        var token = await tokenRepository.GetByIdAsync(command.TokenId, ct);
        if (token is null || token.SessionId != session.Id)
            return Error.NotFound(nameof(TableToken));

        var isOrganizer = session.OrganizerId == currentUser.Id;
        if (!token.CanBeMovedBy(currentUser.Id, isOrganizer))
            return Error.Unauthorized();

        // Без явного Update(token): токен уже отслеживается контекстом (загружен той же сессией
        // DbContext чуть выше) — повторный Update() поверх уже отслеживаемого owned-списка
        // Conditions заставляет EF пометить только что добавленный элемент как Modified вместо
        // Added, что даёт DbUpdateConcurrencyException (0 строк затронуто вместо 1) при INSERT
        // в token_conditions.
        token.ApplyCondition(command.Slug, command.Name, command.Value);
        await unitOfWork.SaveChangesAsync(ct);

        await notifier.NotifyTokenUpdatedAsync(command.SessionId, TableTokenMapper.ToDto(token, canMove: true), ct);

        return Result.Success();
    }
}
