using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.SetFogSettings;

internal sealed class SetFogSettingsCommandHandler(
    IGameSessionRepository sessionRepository,
    ISceneRepository sceneRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<SetFogSettingsCommand, Result>
{
    public async Task<Result> Handle(SetFogSettingsCommand command, CancellationToken ct)
    {
        if (command.VisionRadiusFeet is < 5 or > 500)
            return Error.Validation("Fog.InvalidRadius", "Радиус зрения должен быть от 5 до 500 футов.");

        var resolved = await ActiveSceneResolver.ResolveForGmAsync(
            sessionRepository, sceneRepository, new GameSessionId(command.SessionId), currentUser.Id, ct);
        if (resolved.IsFailure)
            return resolved.Error!;

        var scene = resolved.Value!.Scene;
        scene.SetFogSettings(command.Enabled, command.VisionRadiusFeet);
        sceneRepository.Update(scene);
        await unitOfWork.SaveChangesAsync(ct);
        await notifier.NotifyFogSettingsChangedAsync(command.SessionId, command.Enabled, command.VisionRadiusFeet, ct);

        return Result.Success();
    }
}
