using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Characters.Commands.UploadAvatar;

public sealed record UploadAvatarCommand(
    Guid CharacterId,
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSize
) : IRequest<Result<string>>;
