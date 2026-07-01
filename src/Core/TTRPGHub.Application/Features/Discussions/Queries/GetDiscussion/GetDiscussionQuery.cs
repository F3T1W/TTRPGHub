using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities.Discussions;

namespace TTRPGHub.Features.Discussions.Queries.GetDiscussion;

public sealed record GetDiscussionQuery(string EntityType, string EntitySlug)
    : IRequest<Result<List<DiscussionPostDto>>>;

public sealed record DiscussionPostDto(
    Guid Id,
    Guid AuthorId,
    string AuthorUsername,
    string? AuthorAvatarUrl,
    string Content,
    Guid? ParentId,
    int LikeCount,
    bool IsLikedByMe,
    bool IsOwn,
    DateTime CreatedAt,
    List<DiscussionPostDto> Replies);
