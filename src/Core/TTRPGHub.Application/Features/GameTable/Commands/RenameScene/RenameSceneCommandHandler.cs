using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.RenameScene;

internal sealed class RenameSceneCommandHandler(
    IGameSessionRepository sessionRepository,
    ISceneRepository sceneRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<RenameSceneCommand, Result>
{
    public async Task<Result> Handle(RenameSceneCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Length > 200)
            return Error.Validation("Scene.InvalidName", "Название сцены некорректно.");

        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));
        if (session.OrganizerId != currentUser.Id)
            return Error.Unauthorized();

        var scene = await sceneRepository.GetByIdAsync(command.SceneId, ct);
        if (scene is null || scene.SessionId != session.Id)
            return Error.NotFound(nameof(Scene));

        scene.Rename(command.Name.Trim());
        sceneRepository.Update(scene);
        await unitOfWork.SaveChangesAsync(ct);
        await notifier.NotifyActiveSceneChangedAsync(command.SessionId, ct);

        return Result.Success();
    }
}
