using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.UploadTokenImage;

public sealed record UploadTokenImageCommand(
    Guid SessionId,
    Guid TokenId,
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSize
) : IRequest<Result<string>>;
