using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Homebrew.Commands.ToggleHomebrewLike;

public sealed record ToggleHomebrewLikeCommand(Guid ItemId) : IRequest<Result<ToggleHomebrewLikeResponse>>;
public sealed record ToggleHomebrewLikeResponse(bool Liked, int LikeCount);
