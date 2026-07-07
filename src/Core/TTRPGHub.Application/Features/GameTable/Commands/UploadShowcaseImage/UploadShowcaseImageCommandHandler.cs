using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.UploadShowcaseImage;

internal sealed class UploadShowcaseImageCommandHandler(
    IGameSessionRepository sessionRepository,
    ISceneRepository sceneRepository,
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

        var resolved = await ActiveSceneResolver.ResolveForGmAsync(
            sessionRepository, sceneRepository, new GameSessionId(command.SessionId), currentUser.Id, ct);
        if (resolved.IsFailure)
            return resolved.Error!;

        var scene = resolved.Value!.Scene;

        await storage.EnsureBucketExistsAsync(Bucket, ct);

        var ext = command.ContentType switch
        {
            "image/png"  => "png",
            "image/webp" => "webp",
            _            => "jpg"
        };
        var objectName = $"{command.SessionId}/{Guid.NewGuid():N}.{ext}";
        var url = await storage.UploadAsync(Bucket, objectName, command.FileStream, command.ContentType, ct);

        scene.SetShowcaseImage(url);
        sceneRepository.Update(scene);
        await unitOfWork.SaveChangesAsync(ct);

        await notifier.NotifyShowcaseImageChangedAsync(command.SessionId, url, ct);
        return url;
    }
}
