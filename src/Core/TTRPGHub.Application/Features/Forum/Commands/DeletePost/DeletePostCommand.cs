using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Forum.Commands.DeletePost;

public sealed record DeletePostCommand(Guid PostId) : IRequest<Result>;
