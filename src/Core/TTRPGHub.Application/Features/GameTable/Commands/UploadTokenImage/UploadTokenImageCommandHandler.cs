using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.UploadTokenImage;

internal sealed class UploadTokenImageCommandHandler(
    IGameSessionRepository sessionRepository,
    ITableTokenRepository tokenRepository,
    IStorageService storage,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<UploadTokenImageCommand, Result<string>>
{
    private const string Bucket = "token-images";
    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    public async Task<Result<string>> Handle(UploadTokenImageCommand command, CancellationToken ct)
    {
        if (!AllowedTypes.Contains(command.ContentType))
            return Error.Validation("TokenImage", "Допустимые форматы: JPEG, PNG, WebP.");

        if (command.FileSize > MaxBytes)
            return Error.Validation("TokenImage", "Максимальный размер файла — 5 МБ.");

        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        var token = await tokenRepository.GetByIdAsync(command.TokenId, ct);
        if (token is null || token.SessionId != session.Id)
            return Error.NotFound(nameof(TableToken));

        var isOrganizer = session.OrganizerId == currentUser.Id;
        if (!token.CanBeMovedBy(currentUser.Id, isOrganizer))
            return Error.Unauthorized();

        await storage.EnsureBucketExistsAsync(Bucket, ct);

        var ext = command.ContentType switch
        {
            "image/png"  => "png",
            "image/webp" => "webp",
            _            => "jpg"
        };
        var objectName = $"{command.TokenId}/{Guid.NewGuid():N}.{ext}";

        var url = await storage.UploadAsync(Bucket, objectName, command.FileStream, command.ContentType, ct);

        token.SetImage(url);
        tokenRepository.Update(token);
        await unitOfWork.SaveChangesAsync(ct);

        await notifier.NotifyTokenUpdatedAsync(command.SessionId, TableTokenMapper.ToDto(token, canMove: true), ct);

        return url;
    }
}
