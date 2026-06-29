using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Forum;
using TTRPGHub.Repositories.Forum;
using TTRPGHub.Repositories;
using TTRPGHub.Common.Interfaces;

namespace TTRPGHub.Features.Forum.Commands.CreatePost;

internal sealed class CreatePostCommandHandler(
    IForumTopicRepository topics,
    IForumPostRepository posts,
    IUnitOfWork uow,
    ICurrentUser currentUser)
    : IRequestHandler<CreatePostCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreatePostCommand request, CancellationToken ct)
    {
        var topic = await topics.GetByIdAsync(ForumTopicId.From(request.TopicId), ct);
        if (topic is null)
            return Error.NotFound(nameof(topic));

        if (topic.IsLocked)
            return Error.Validation("Topic", "Тема закрыта для ответов");

        var post = ForumPost.Create(topic.Id, currentUser.Id, request.Content);
        posts.Add(post);
        topic.UpdateLastPostAt(post.CreatedAt);

        await uow.SaveChangesAsync(ct);
        return Result<Guid>.Success(post.Id.Value);
    }
}
