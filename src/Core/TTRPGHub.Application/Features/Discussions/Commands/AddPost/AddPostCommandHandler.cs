using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities.Discussions;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Discussions.Commands.AddPost;

internal sealed class AddPostCommandHandler(
    IDiscussionRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<AddPostCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddPostCommand cmd, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated) return Error.Unauthorized();
        if (!Enum.TryParse<DiscussionEntityType>(cmd.EntityType, true, out var entityType))
            return Error.Validation("EntityType", "Неверный тип.");
        if (string.IsNullOrWhiteSpace(cmd.Content) || cmd.Content.Length > 2000)
            return Error.Validation("Content", "Сообщение от 1 до 2000 символов.");

        DiscussionPostId? parentId = cmd.ParentId.HasValue ? DiscussionPostId.From(cmd.ParentId.Value) : null;
        if (parentId.HasValue)
        {
            var parent = await repository.GetByIdAsync(parentId.Value, ct);
            if (parent is null) return Error.NotFound("ParentPost");
        }

        var post = DiscussionPost.Create(entityType, cmd.EntitySlug, currentUser.Id, cmd.Content, parentId);
        await repository.AddAsync(post, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return post.Id.Value;
    }
}
