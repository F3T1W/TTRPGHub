using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Shared;

// J.4 — большинство команд стола (карта/сетка/туман/стены/свет/бой) раньше читали/писали поля
// прямо на GameSession; теперь эти поля живут на активной сцене сессии (Scene). Общий путь
// "загрузить сессию → проверить, что это ГМ → загрузить активную сцену" вынесен сюда, чтобы не
// повторять его в каждом обработчике почти без изменений.
internal static class ActiveSceneResolver
{
    internal static async Task<Result<(GameSession Session, Scene Scene)>> ResolveForGmAsync(
        IGameSessionRepository sessionRepository, ISceneRepository sceneRepository,
        GameSessionId sessionId, UserId requesterId, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId, ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));
        if (session.OrganizerId != requesterId)
            return Error.Unauthorized();
        if (session.ActiveSceneId is not { } activeSceneId)
            return Error.Validation("Scene.NoActiveScene", "У сессии нет активной сцены.");

        var scene = await sceneRepository.GetByIdAsync(activeSceneId, ct);
        if (scene is null)
            return Error.NotFound(nameof(Scene));

        return (session, scene);
    }
}
