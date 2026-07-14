using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Macros.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Macros.Commands.ShareMacro;

internal sealed class ShareMacroCommandHandler(
    IGameSessionRepository sessionRepository,
    IMacroRepository macroRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ITableNotifier notifier
) : IRequestHandler<ShareMacroCommand, Result>
{
    public async Task<Result> Handle(ShareMacroCommand command, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null) return Error.NotFound(nameof(GameSession));
        if (session.OrganizerId != currentUser.Id) return Error.Unauthorized();

        var macro = await macroRepository.GetByIdAsync(command.MacroId, ct);
        if (macro is null) return Error.NotFound(nameof(Macro));
        if (macro.OwnerId != currentUser.Id) return Error.Unauthorized();

        session.ShareMacro(command.MacroId);
        sessionRepository.Update(session);
        await unitOfWork.SaveChangesAsync(ct);

        var shared = await macroRepository.GetByIdsAsync(session.SharedMacroIds, ct);
        await notifier.NotifySharedMacrosChangedAsync(
            command.SessionId, shared.Select(MacroMapper.ToDto).ToList(), ct);

        return Result.Success();
    }
}
