using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.SetSceneEnvironment;

internal sealed class SetSceneEnvironmentCommandHandler(
    IGameSessionRepository sessionRepository,
    ISceneRepository sceneRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<SetSceneEnvironmentCommand, Result>
{
    private static readonly HashSet<string> AllowedLighting = new(StringComparer.OrdinalIgnoreCase)
        { "bright", "dim-light", "darkness" };

    public async Task<Result> Handle(SetSceneEnvironmentCommand command, CancellationToken ct)
    {
        if (!AllowedLighting.Contains(command.AmbientLighting))
            return Error.Validation("Scene.InvalidLighting", "Освещение: bright, dim-light или darkness.");

        var resolved = await ActiveSceneResolver.ResolveForGmAsync(
            sessionRepository, sceneRepository, new GameSessionId(command.SessionId), currentUser.Id, ct);
        if (resolved.IsFailure)
            return resolved.Error!;

        var scene = resolved.Value!.Scene;
        scene.SetEnvironment(command.TerrainTagsJson, command.AmbientLighting);
        sceneRepository.Update(scene);
        await unitOfWork.SaveChangesAsync(ct);
        await notifier.NotifySceneEnvironmentChangedAsync(
            command.SessionId, command.TerrainTagsJson, command.AmbientLighting, ct);

        return Result.Success();
    }
}
