using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.SeekTableAudio;

internal sealed class SeekTableAudioCommandHandler(
    IGameSessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<SeekTableAudioCommand, Result>
{
    public async Task<Result> Handle(SeekTableAudioCommand command, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        var error = session.SeekAudio(currentUser.Id, command.PositionSeconds);
        if (error is not null)
            return error;

        sessionRepository.Update(session);
        await unitOfWork.SaveChangesAsync(ct);

        await notifier.NotifyAudioStateChangedAsync(command.SessionId, AudioStateMapper.ToDto(session), ct);
        return Result.Success();
    }
}
