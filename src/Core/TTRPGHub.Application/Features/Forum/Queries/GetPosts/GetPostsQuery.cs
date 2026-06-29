using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Forum.Queries.GetPosts;

public sealed record GetPostsQuery(Guid TopicId, int Page = 1, int PageSize = 20)
    : IRequest<Result<ForumTopicDetailDto>>;

public sealed record ForumTopicDetailDto(
    Guid Id,
    string Title,
    bool IsPinned,
    bool IsLocked,
    string CategorySlug,
    string CategoryName,
    PagedPostsDto Posts);

public sealed record PagedPostsDto(List<ForumPostDto> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}

public sealed record ForumPostDto(
    Guid Id,
    Guid AuthorId,
    string AuthorUsername,
    string? AuthorAvatarUrl,
    string Content,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int LikeCount,
    bool LikedByMe);
