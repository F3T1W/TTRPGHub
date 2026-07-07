using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.RemoveTokenCondition;

internal sealed class RemoveTokenConditionCommandHandler(
    IGameSessionRepository sessionRepository,
    ITableTokenRepository tokenRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<RemoveTokenConditionCommand, Result>
{
    public async Task<Result> Handle(RemoveTokenConditionCommand command, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        var token = await tokenRepository.GetByIdAsync(command.TokenId, ct);
        if (token is null || token.SessionId != session.Id)
            return Error.NotFound(nameof(TableToken));

        var isOrganizer = session.OrganizerId == currentUser.Id;
        if (!token.CanBeMovedBy(currentUser.Id, isOrganizer))
            return Error.Unauthorized();

        // См. комментарий в ApplyTokenConditionCommandHandler — без явного Update() по той же причине.
        token.RemoveCondition(command.Slug);
        await unitOfWork.SaveChangesAsync(ct);

        await notifier.NotifyTokenUpdatedAsync(command.SessionId, TableTokenMapper.ToDto(token, canMove: true), ct);

        return Result.Success();
    }
}
