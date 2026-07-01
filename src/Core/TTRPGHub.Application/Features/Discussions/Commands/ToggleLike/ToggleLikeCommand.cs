using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Discussions.Commands.ToggleLike;

public sealed record ToggleLikeCommand(Guid PostId) : IRequest<Result<bool>>;
