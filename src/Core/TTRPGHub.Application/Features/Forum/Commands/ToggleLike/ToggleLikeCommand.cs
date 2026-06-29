using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Forum.Commands.ToggleLike;

public sealed record ToggleLikeCommand(Guid PostId) : IRequest<Result<ToggleLikeResponse>>;

public sealed record ToggleLikeResponse(bool Liked, int LikeCount);
