using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Characters.Commands.UploadAvatar;

internal sealed class UploadAvatarCommandHandler(
    ICharacterRepository characterRepository,
    ICurrentUser currentUser,
    IStorageService storage,
    IUnitOfWork unitOfWork,
    ICacheService cache
) : IRequestHandler<UploadAvatarCommand, Result<string>>
{
    private const string Bucket = "avatars";
    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    public async Task<Result<string>> Handle(UploadAvatarCommand command, CancellationToken ct)
    {
        if (!AllowedTypes.Contains(command.ContentType))
            return Error.Validation("Avatar", "Допустимые форматы: JPEG, PNG, WebP.");

        if (command.FileSize > MaxBytes)
            return Error.Validation("Avatar", "Максимальный размер файла — 5 МБ.");

        var character = await characterRepository.GetByIdAsync(new CharacterId(command.CharacterId), ct);
        if (character is null)
            return Error.NotFound(nameof(Character));

        if (character.OwnerId != currentUser.Id)
            return Error.Unauthorized();

        await storage.EnsureBucketExistsAsync(Bucket, ct);

        var ext = command.ContentType switch
        {
            "image/png"  => "png",
            "image/webp" => "webp",
            _            => "jpg"
        };
        var objectName = $"{command.CharacterId}/{Guid.NewGuid():N}.{ext}";

        var url = await storage.UploadAsync(Bucket, objectName, command.FileStream, command.ContentType, ct);

        character.SetAvatar(url);
        await unitOfWork.SaveChangesAsync(ct);

        await cache.RemoveAsync($"characters:{command.CharacterId}", ct);
        await cache.RemoveAsync($"characters:owner:{currentUser.Id}", ct);

        return url;
    }
}
