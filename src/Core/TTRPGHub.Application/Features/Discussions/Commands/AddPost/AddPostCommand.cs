using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Discussions.Commands.AddPost;

public sealed record AddPostCommand(string EntityType, string EntitySlug, string Content, Guid? ParentId)
    : IRequest<Result<Guid>>;
