using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.PlayTableAudio;

internal sealed class PlayTableAudioCommandHandler(
    IGameSessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<PlayTableAudioCommand, Result>
{
    public async Task<Result> Handle(PlayTableAudioCommand command, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        var error = session.PlayAudio(currentUser.Id, command.PositionSeconds);
        if (error is not null)
            return error;

        sessionRepository.Update(session);
        await unitOfWork.SaveChangesAsync(ct);

        await notifier.NotifyAudioStateChangedAsync(command.SessionId, AudioStateMapper.ToDto(session), ct);
        return Result.Success();
    }
}
