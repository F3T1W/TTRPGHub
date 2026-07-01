using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Discussions.Commands.DeletePost;

public sealed record DeletePostCommand(Guid PostId) : IRequest<Result>;
