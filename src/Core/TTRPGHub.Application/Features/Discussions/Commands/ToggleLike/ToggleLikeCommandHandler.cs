using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities.Discussions;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Discussions.Commands.ToggleLike;

internal sealed class ToggleLikeCommandHandler(
    IDiscussionRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<ToggleLikeCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ToggleLikeCommand cmd, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated) return Error.Unauthorized();
        var postId = DiscussionPostId.From(cmd.PostId);
        var post = await repository.GetByIdAsync(postId, ct);
        if (post is null) return Error.NotFound(nameof(DiscussionPost));

        var existing = await repository.GetLikeAsync(postId, currentUser.Id, ct);
        if (existing is not null)
        {
            repository.RemoveLike(existing);
            post.RemoveLike();
            await unitOfWork.SaveChangesAsync(ct);
            return false;
        }
        var like = DiscussionLike.Create(postId, currentUser.Id);
        await repository.AddLikeAsync(like, ct);
        post.AddLike();
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}
