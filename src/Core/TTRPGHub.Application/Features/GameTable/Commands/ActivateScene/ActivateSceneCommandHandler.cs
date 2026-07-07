using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.ActivateScene;

internal sealed class ActivateSceneCommandHandler(
    IGameSessionRepository sessionRepository,
    ISceneRepository sceneRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<ActivateSceneCommand, Result>
{
    public async Task<Result> Handle(ActivateSceneCommand command, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        var scene = await sceneRepository.GetByIdAsync(command.SceneId, ct);
        if (scene is null || scene.SessionId != session.Id)
            return Error.NotFound(nameof(Scene));

        var error = session.SetActiveScene(currentUser.Id, command.SceneId);
        if (error is not null)
            return error;

        sessionRepository.Update(session);
        await unitOfWork.SaveChangesAsync(ct);
        await notifier.NotifyActiveSceneChangedAsync(command.SessionId, ct);

        return Result.Success();
    }
}
