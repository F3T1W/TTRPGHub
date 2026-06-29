using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Forum.Commands.CreatePost;

public sealed record CreatePostCommand(Guid TopicId, string Content) : IRequest<Result<Guid>>;
