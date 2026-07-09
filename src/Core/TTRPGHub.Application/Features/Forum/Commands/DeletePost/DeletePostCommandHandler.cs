using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Forum;
using TTRPGHub.Entities.Moderation;
using TTRPGHub.Repositories;
using TTRPGHub.Repositories.Forum;

namespace TTRPGHub.Features.Forum.Commands.DeletePost;

internal sealed class DeletePostCommandHandler(
    IForumPostRepository posts,
    IModerationLogRepository moderationLog,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork
) : IRequestHandler<DeletePostCommand, Result>
{
    public async Task<Result> Handle(DeletePostCommand request, CancellationToken ct)
    {
        var post = await posts.GetByIdAsync(ForumPostId.From(request.PostId), ct);
        if (post is null)
            return Error.NotFound(nameof(ForumPost));

        var isModerator = currentUser.Role is UserRole.Moderator or UserRole.Admin;
        if (post.AuthorId != currentUser.Id && !isModerator)
            return Error.Forbidden();

        posts.Remove(post);

        if (isModerator && post.AuthorId != currentUser.Id)
        {
            await moderationLog.AddAsync(ModerationLogEntry.Create(
                currentUser.Id, "DeletePost", nameof(ForumPost), post.Id.Value), ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
