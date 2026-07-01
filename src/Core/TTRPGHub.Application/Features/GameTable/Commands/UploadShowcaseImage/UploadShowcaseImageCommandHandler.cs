using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.UploadShowcaseImage;

internal sealed class UploadShowcaseImageCommandHandler(
    IGameSessionRepository sessionRepository,
    IStorageService storage,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<UploadShowcaseImageCommand, Result<string>>
{
    private const string Bucket = "table-showcase";
    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxBytes = 10 * 1024 * 1024; // 10 MB

    public async Task<Result<string>> Handle(UploadShowcaseImageCommand command, CancellationToken ct)
    {
        if (!AllowedTypes.Contains(command.ContentType))
            return Error.Validation("ShowcaseImage", "Допустимые форматы: JPEG, PNG, WebP.");

        if (command.FileSize > MaxBytes)
            return Error.Validation("ShowcaseImage", "Максимальный размер файла — 10 МБ.");

        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        if (session.OrganizerId != currentUser.Id)
            return Error.Unauthorized();

        await storage.EnsureBucketExistsAsync(Bucket, ct);

        var ext = command.ContentType switch
        {
            "image/png"  => "png",
            "image/webp" => "webp",
            _            => "jpg"
        };
        var objectName = $"{command.SessionId}/{Guid.NewGuid():N}.{ext}";
        var url = await storage.UploadAsync(Bucket, objectName, command.FileStream, command.ContentType, ct);

        var error = session.SetShowcaseImage(currentUser.Id, url);
        if (error is not null)
            return error;

        sessionRepository.Update(session);
        await unitOfWork.SaveChangesAsync(ct);

        await notifier.NotifyShowcaseImageChangedAsync(command.SessionId, url, ct);
        return url;
    }
}
