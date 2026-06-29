using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Forum;
using TTRPGHub.Repositories.Forum;
using TTRPGHub.Repositories;
using TTRPGHub.Common.Interfaces;

namespace TTRPGHub.Features.Forum.Commands.ToggleLike;

internal sealed class ToggleLikeCommandHandler(
    IForumPostRepository posts,
    IUnitOfWork uow,
    ICurrentUser currentUser)
    : IRequestHandler<ToggleLikeCommand, Result<ToggleLikeResponse>>
{
    public async Task<Result<ToggleLikeResponse>> Handle(ToggleLikeCommand request, CancellationToken ct)
    {
        var postId = ForumPostId.From(request.PostId);
        var userId = currentUser.Id;

        var post = await posts.GetByIdAsync(postId, ct);
        if (post is null)
            return Error.NotFound(nameof(post));

        var hasLike = await posts.HasLikeAsync(postId, userId, ct);
        bool liked;

        if (hasLike)
        {
            var like = post.Likes.First(l => l.UserId == userId);
            posts.RemoveLike(like);
            liked = false;
        }
        else
        {
            posts.AddLike(ForumPostLike.Create(postId, userId));
            liked = true;
        }

        await uow.SaveChangesAsync(ct);

        var newCount = post.Likes.Count + (liked ? 1 : -1);
        return Result<ToggleLikeResponse>.Success(new ToggleLikeResponse(liked, newCount));
    }
}
