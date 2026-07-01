using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Discussions;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Discussions.Commands.DeletePost;

internal sealed class DeletePostCommandHandler(
    IDiscussionRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<DeletePostCommand, Result>
{
    public async Task<Result> Handle(DeletePostCommand cmd, CancellationToken ct)
    {
        var post = await repository.GetByIdAsync(DiscussionPostId.From(cmd.PostId), ct);
        if (post is null) return Error.NotFound(nameof(DiscussionPost));
        var isModerator = currentUser.Role is UserRole.Moderator or UserRole.Admin;
        if (post.AuthorId != currentUser.Id && !isModerator) return Error.Forbidden();
        repository.Remove(post);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
