using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.MoveTableToken;

internal sealed class MoveTableTokenCommandHandler(
    IGameSessionRepository sessionRepository,
    ITableTokenRepository tokenRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<MoveTableTokenCommand, Result>
{
    public async Task<Result> Handle(MoveTableTokenCommand command, CancellationToken ct)
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

        token.Move(command.X, command.Y);
        tokenRepository.Update(token);
        await unitOfWork.SaveChangesAsync(ct);

        await notifier.NotifyTokenMovedAsync(command.SessionId, token.Id, token.X, token.Y, ct);
        return Result.Success();
    }
}
