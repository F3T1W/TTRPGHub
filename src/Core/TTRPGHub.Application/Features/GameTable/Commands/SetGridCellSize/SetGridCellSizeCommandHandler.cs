using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.SetGridCellSize;

internal sealed class SetGridCellSizeCommandHandler(
    IGameSessionRepository sessionRepository,
    ISceneRepository sceneRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<SetGridCellSizeCommand, Result>
{
    public async Task<Result> Handle(SetGridCellSizeCommand command, CancellationToken ct)
    {
        if (command.Px is < 10 or > 300)
            return Error.Validation("Grid.InvalidSize", "Размер клетки должен быть от 10 до 300 px.");

        var resolved = await ActiveSceneResolver.ResolveForGmAsync(
            sessionRepository, sceneRepository, new GameSessionId(command.SessionId), currentUser.Id, ct);
        if (resolved.IsFailure)
            return resolved.Error!;

        var scene = resolved.Value!.Scene;
        scene.SetGridCellSize(command.Px);
        sceneRepository.Update(scene);
        await unitOfWork.SaveChangesAsync(ct);
        await notifier.NotifyGridCellSizeChangedAsync(command.SessionId, command.Px, ct);

        return Result.Success();
    }
}
