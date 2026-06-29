using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Forum.Queries.GetTopics;

public sealed record GetTopicsQuery(string CategorySlug, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<ForumTopicDto>>>;

public sealed record ForumTopicDto(
    Guid Id,
    string Title,
    Guid AuthorId,
    string AuthorUsername,
    bool IsPinned,
    bool IsLocked,
    DateTime CreatedAt,
    DateTime? LastPostAt,
    int PostCount);

public sealed record PagedResult<T>(List<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}
