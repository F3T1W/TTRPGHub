using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.DeleteScene;

// Нет FK-каскада между table_tokens.scene_id и scenes.id (TableToken не хранит навигацию на
// Scene, только Guid) — токены удалённой сцены пришлось бы чистить руками при рассинхроне,
// поэтому обработчик удаляет их явно перед удалением самой сцены.
internal sealed class DeleteSceneCommandHandler(
    IGameSessionRepository sessionRepository,
    ISceneRepository sceneRepository,
    ITableTokenRepository tokenRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<DeleteSceneCommand, Result>
{
    public async Task<Result> Handle(DeleteSceneCommand command, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));
        if (session.OrganizerId != currentUser.Id)
            return Error.Unauthorized();

        var scene = await sceneRepository.GetByIdAsync(command.SceneId, ct);
        if (scene is null || scene.SessionId != session.Id)
            return Error.NotFound(nameof(Scene));

        var allScenes = await sceneRepository.GetBySessionAsync(session.Id, ct);
        if (allScenes.Count <= 1)
            return Error.Validation("Scene.LastOne", "Нельзя удалить последнюю сцену сессии.");

        var tokens = await tokenRepository.GetBySceneAsync(scene.Id, ct);
        foreach (var token in tokens)
            tokenRepository.Remove(token);

        if (session.ActiveSceneId == scene.Id)
        {
            var fallback = allScenes.First(s => s.Id != scene.Id);
            session.SetActiveScene(currentUser.Id, fallback.Id);
            sessionRepository.Update(session);
        }

        sceneRepository.Remove(scene);
        await unitOfWork.SaveChangesAsync(ct);
        await notifier.NotifyActiveSceneChangedAsync(command.SessionId, ct);

        return Result.Success();
    }
}
