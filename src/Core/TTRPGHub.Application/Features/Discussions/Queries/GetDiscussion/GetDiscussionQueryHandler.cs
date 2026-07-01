using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities.Discussions;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Discussions.Queries.GetDiscussion;

internal sealed class GetDiscussionQueryHandler(
    IDiscussionRepository repository,
    ICurrentUser currentUser
) : IRequestHandler<GetDiscussionQuery, Result<List<DiscussionPostDto>>>
{
    public async Task<Result<List<DiscussionPostDto>>> Handle(GetDiscussionQuery q, CancellationToken ct)
    {
        if (!Enum.TryParse<DiscussionEntityType>(q.EntityType, true, out var entityType))
            return Error.Validation("EntityType", "Неверный тип сущности.");

        var posts = await repository.GetByEntityAsync(entityType, q.EntitySlug, ct);

        var dtos = posts.ToDictionary(
            p => p.Id,
            p => new DiscussionPostDto(
                p.Id.Value, p.AuthorId.Value, p.Author.Username, p.Author.Profile.AvatarUrl,
                p.Content, p.ParentId?.Value,
                p.LikeCount,
                p.Likes.Any(l => l.UserId == currentUser.Id),
                p.AuthorId == currentUser.Id,
                p.CreatedAt, []));

        var roots = new List<DiscussionPostDto>();
        foreach (var dto in dtos.Values)
        {
            if (dto.ParentId is null)
                roots.Add(dto);
            else if (dtos.TryGetValue(new DiscussionPostId(dto.ParentId.Value), out var parent))
                parent.Replies.Add(dto);
        }
        return roots;
    }
}
