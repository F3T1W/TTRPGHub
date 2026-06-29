using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Repositories.Forum;

namespace TTRPGHub.Features.Forum.Queries.GetTopics;

internal sealed class GetTopicsQueryHandler(
    IForumCategoryRepository categories,
    IForumTopicRepository topics)
    : IRequestHandler<GetTopicsQuery, Result<PagedResult<ForumTopicDto>>>
{
    public async Task<Result<PagedResult<ForumTopicDto>>> Handle(GetTopicsQuery request, CancellationToken ct)
    {
        var category = await categories.GetBySlugAsync(request.CategorySlug, ct);
        if (category is null)
            return Error.NotFound(nameof(category));

        var (items, total) = await topics.GetByCategoryAsync(
            category.Id, request.Page, request.PageSize, ct);

        var dtos = items.Select(t => new ForumTopicDto(
            t.Id.Value,
            t.Title,
            t.AuthorId.Value,
            t.Author.Username,
            t.IsPinned,
            t.IsLocked,
            t.CreatedAt,
            t.LastPostAt,
            t.Posts.Count))
            .ToList();

        return Result<PagedResult<ForumTopicDto>>.Success(
            new PagedResult<ForumTopicDto>(dtos, total, request.Page, request.PageSize));
    }
}
