using System.Text.Json;
using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.SetTokenVisibility;

internal sealed class SetTokenVisibilityCommandHandler(
    IGameSessionRepository sessionRepository,
    ITableTokenRepository tokenRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<SetTokenVisibilityCommand, Result>
{
    public async Task<Result> Handle(SetTokenVisibilityCommand command, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        // Видимость токена меняет только GM — иначе игрок мог бы скрыть чужой токен от других.
        if (session.OrganizerId != currentUser.Id)
            return Error.Unauthorized();

        var token = await tokenRepository.GetByIdAsync(command.TokenId, ct);
        if (token is null || token.SessionId != session.Id)
            return Error.NotFound(nameof(TableToken));

        var json = command.VisibleToUserIds is null ? null : JsonSerializer.Serialize(command.VisibleToUserIds);
        token.SetVisibility(json);
        tokenRepository.Update(token);
        await unitOfWork.SaveChangesAsync(ct);

        var dto = TableTokenMapper.ToDto(token, canMove: true);
        await notifier.NotifyTokenVisibilityChangedAsync(command.SessionId, session.OrganizerId.Value, dto, ct);

        return Result.Success();
    }
}
