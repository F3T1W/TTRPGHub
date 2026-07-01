using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.UploadShowcaseImage;

public sealed record UploadShowcaseImageCommand(
    Guid SessionId,
    Stream FileStream,
    string ContentType,
    long FileSize
) : IRequest<Result<string>>;
