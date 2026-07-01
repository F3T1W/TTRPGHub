using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.UploadTableTrack;

internal sealed class UploadTableTrackCommandHandler(
    IGameSessionRepository sessionRepository,
    IStorageService storage,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<UploadTableTrackCommand, Result<string>>
{
    private const string Bucket = "table-audio";
    private static readonly string[] AllowedTypes = ["audio/mpeg", "audio/ogg", "audio/wav", "audio/webm", "audio/mp4"];
    private const long MaxBytes = 30 * 1024 * 1024; // 30 MB

    public async Task<Result<string>> Handle(UploadTableTrackCommand command, CancellationToken ct)
    {
        if (!AllowedTypes.Contains(command.ContentType))
            return Error.Validation("TableTrack", "Допустимые форматы: MP3, OGG, WAV.");

        if (command.FileSize > MaxBytes)
            return Error.Validation("TableTrack", "Максимальный размер файла — 30 МБ.");

        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        if (session.OrganizerId != currentUser.Id)
            return Error.Unauthorized();

        await storage.EnsureBucketExistsAsync(Bucket, ct);

        var ext = command.ContentType switch
        {
            "audio/ogg"  => "ogg",
            "audio/wav"  => "wav",
            "audio/webm" => "webm",
            "audio/mp4"  => "m4a",
            _            => "mp3"
        };
        var objectName = $"{command.SessionId}/{Guid.NewGuid():N}.{ext}";
        var url = await storage.UploadAsync(Bucket, objectName, command.FileStream, command.ContentType, ct);

        var title = Path.GetFileNameWithoutExtension(command.FileName);
        var error = session.SetTrack(currentUser.Id, url, title);
        if (error is not null)
            return error;

        sessionRepository.Update(session);
        await unitOfWork.SaveChangesAsync(ct);

        await notifier.NotifyAudioStateChangedAsync(command.SessionId, AudioStateMapper.ToDto(session), ct);
        return url;
    }
}
