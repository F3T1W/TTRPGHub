using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Forum;
using TTRPGHub.Repositories.Forum;
using TTRPGHub.Common.Interfaces;

namespace TTRPGHub.Features.Forum.Queries.GetPosts;

internal sealed class GetPostsQueryHandler(
    IForumTopicRepository topics,
    IForumPostRepository posts,
    ICurrentUser currentUser)
    : IRequestHandler<GetPostsQuery, Result<ForumTopicDetailDto>>
{
    public async Task<Result<ForumTopicDetailDto>> Handle(GetPostsQuery request, CancellationToken ct)
    {
        var topic = await topics.GetByIdAsync(ForumTopicId.From(request.TopicId), ct);
        if (topic is null)
            return Error.NotFound(nameof(topic));

        UserId? userId = currentUser.IsAuthenticated ? currentUser.Id : null;

        var (items, total) = await posts.GetByTopicAsync(
            topic.Id, request.Page, request.PageSize, userId, ct);

        var postDtos = items.Select(p => new ForumPostDto(
            p.Id.Value,
            p.AuthorId.Value,
            p.Author.Username,
            p.Author.Profile.AvatarUrl,
            p.Content,
            p.CreatedAt,
            p.UpdatedAt,
            p.Likes.Count,
            p.Likes.Any(l => l.UserId == userId)))
            .ToList();

        return Result<ForumTopicDetailDto>.Success(new ForumTopicDetailDto(
            topic.Id.Value,
            topic.Title,
            topic.IsPinned,
            topic.IsLocked,
            topic.Category.Slug,
            topic.Category.Name,
            new PagedPostsDto(postDtos, total, request.Page, request.PageSize)));
    }
}
