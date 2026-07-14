using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Macros.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Macros.Commands.UnshareMacro;

internal sealed class UnshareMacroCommandHandler(
    IGameSessionRepository sessionRepository,
    IMacroRepository macroRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ITableNotifier notifier
) : IRequestHandler<UnshareMacroCommand, Result>
{
    public async Task<Result> Handle(UnshareMacroCommand command, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null) return Error.NotFound(nameof(GameSession));
        if (session.OrganizerId != currentUser.Id) return Error.Unauthorized();

        session.UnshareMacro(command.MacroId);
        sessionRepository.Update(session);
        await unitOfWork.SaveChangesAsync(ct);

        var shared = await macroRepository.GetByIdsAsync(session.SharedMacroIds, ct);
        await notifier.NotifySharedMacrosChangedAsync(
            command.SessionId, shared.Select(MacroMapper.ToDto).ToList(), ct);

        return Result.Success();
    }
}
