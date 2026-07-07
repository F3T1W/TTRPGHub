using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.SetShowcaseImage;

internal sealed class SetShowcaseImageCommandHandler(
    IGameSessionRepository sessionRepository,
    ISceneRepository sceneRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<SetShowcaseImageCommand, Result>
{
    public async Task<Result> Handle(SetShowcaseImageCommand command, CancellationToken ct)
    {
        var resolved = await ActiveSceneResolver.ResolveForGmAsync(
            sessionRepository, sceneRepository, new GameSessionId(command.SessionId), currentUser.Id, ct);
        if (resolved.IsFailure)
            return resolved.Error!;

        var scene = resolved.Value!.Scene;
        scene.SetShowcaseImage(command.ImageUrl);
        sceneRepository.Update(scene);
        await unitOfWork.SaveChangesAsync(ct);

        await notifier.NotifyShowcaseImageChangedAsync(command.SessionId, command.ImageUrl, ct);
        return Result.Success();
    }
}
