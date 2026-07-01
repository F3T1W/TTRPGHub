using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.UploadTableTrack;

public sealed record UploadTableTrackCommand(
    Guid SessionId,
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSize
) : IRequest<Result<string>>;
