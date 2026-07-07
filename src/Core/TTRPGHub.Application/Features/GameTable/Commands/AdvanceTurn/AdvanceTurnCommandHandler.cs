using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.AdvanceTurn;

internal sealed class AdvanceTurnCommandHandler(
    IGameSessionRepository sessionRepository,
    ISceneRepository sceneRepository,
    ITableTokenRepository tokenRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<AdvanceTurnCommand, Result>
{
    public async Task<Result> Handle(AdvanceTurnCommand command, CancellationToken ct)
    {
        var resolved = await ActiveSceneResolver.ResolveForGmAsync(
            sessionRepository, sceneRepository, new GameSessionId(command.SessionId), currentUser.Id, ct);
        if (resolved.IsFailure)
            return resolved.Error!;

        var scene = resolved.Value!.Scene;
        if (!scene.CombatActive)
            return Error.Validation("Combat.NotActive", "Бой не начат.");

        var tokens = await tokenRepository.GetBySceneAsync(scene.Id, ct);
        var order = InitiativeOrder.Sorted(tokens);
        if (order.Count == 0)
            return Error.Validation("Combat.NoInitiative", "Ни у одного токена не задана инициатива.");

        var currentIndex = order.FindIndex(t => t.Id == scene.CombatTurnTokenId);
        var round = scene.CombatRound;
        int nextIndex;

        if (command.Forward)
        {
            nextIndex = currentIndex + 1;
            if (nextIndex >= order.Count) { nextIndex = 0; round++; }
        }
        else
        {
            nextIndex = currentIndex < 0 ? order.Count - 1 : currentIndex - 1;
            if (nextIndex < 0) { nextIndex = order.Count - 1; round = Math.Max(1, round - 1); }
        }

        scene.SetCombatTurn(order[nextIndex].Id, round);

        sceneRepository.Update(scene);
        await unitOfWork.SaveChangesAsync(ct);
        await notifier.NotifyCombatStateChangedAsync(command.SessionId, true, scene.CombatRound, scene.CombatTurnTokenId, ct);

        return Result.Success();
    }
}
