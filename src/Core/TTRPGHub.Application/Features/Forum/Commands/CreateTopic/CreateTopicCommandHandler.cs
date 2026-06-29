using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Forum;
using TTRPGHub.Repositories.Forum;
using TTRPGHub.Repositories;
using TTRPGHub.Common.Interfaces;

namespace TTRPGHub.Features.Forum.Commands.CreateTopic;

internal sealed class CreateTopicCommandHandler(
    IForumCategoryRepository categories,
    IForumTopicRepository topics,
    IForumPostRepository posts,
    IUnitOfWork uow,
    ICurrentUser currentUser)
    : IRequestHandler<CreateTopicCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateTopicCommand request, CancellationToken ct)
    {
        var category = await categories.GetByIdAsync(ForumCategoryId.From(request.CategoryId), ct);
        if (category is null)
            return Error.NotFound(nameof(category));

        var authorId = currentUser.Id;
        var topic = ForumTopic.Create(category.Id, authorId, request.Title);
        topics.Add(topic);

        var firstPost = ForumPost.Create(topic.Id, authorId, request.FirstPostContent);
        posts.Add(firstPost);
        topic.UpdateLastPostAt(firstPost.CreatedAt);

        await uow.SaveChangesAsync(ct);
        return Result<Guid>.Success(topic.Id.Value);
    }
}
