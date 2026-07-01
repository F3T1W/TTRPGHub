using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.SetShowcaseImage;

internal sealed class SetShowcaseImageCommandHandler(
    IGameSessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<SetShowcaseImageCommand, Result>
{
    public async Task<Result> Handle(SetShowcaseImageCommand command, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        var error = session.SetShowcaseImage(currentUser.Id, command.ImageUrl);
        if (error is not null)
            return error;

        sessionRepository.Update(session);
        await unitOfWork.SaveChangesAsync(ct);

        await notifier.NotifyShowcaseImageChangedAsync(command.SessionId, command.ImageUrl, ct);
        return Result.Success();
    }
}
