using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.AddTableToken;

internal sealed class AddTableTokenCommandHandler(
    IGameSessionRepository sessionRepository,
    ITableTokenRepository tokenRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<AddTableTokenCommand, Result<TableTokenDto>>
{
    public async Task<Result<TableTokenDto>> Handle(AddTableTokenCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Label) || command.Label.Length > 100)
            return Error.Validation("TableToken.Invalid", "Название жетона некорректно.");

        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        if (session.OrganizerId != currentUser.Id)
            return Error.Unauthorized();

        UserId? ownerId = command.OwnerUserId is { } id ? new UserId(id) : null;
        if (ownerId is not null && !session.IsParticipant(ownerId.Value))
            return Error.Validation("TableToken.InvalidOwner", "Владелец жетона должен быть участником сессии.");

        var token = TableToken.Create(
            session.Id, command.Label.Trim(), command.ImageUrl, command.Color,
            command.X, command.Y, ownerId);

        await tokenRepository.AddAsync(token, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var dto = new TableTokenDto(token.Id, token.Label, token.ImageUrl, token.Color, token.X, token.Y, token.OwnerId?.Value, true);
        await notifier.NotifyTokenAddedAsync(command.SessionId, dto, ct);

        return dto;
    }
}
