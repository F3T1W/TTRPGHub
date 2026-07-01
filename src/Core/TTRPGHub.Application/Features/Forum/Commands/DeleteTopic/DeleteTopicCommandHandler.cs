using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Forum;
using TTRPGHub.Repositories;
using TTRPGHub.Repositories.Forum;

namespace TTRPGHub.Features.Forum.Commands.DeleteTopic;

internal sealed class DeleteTopicCommandHandler(
    IForumTopicRepository topics,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork
) : IRequestHandler<DeleteTopicCommand, Result>
{
    public async Task<Result> Handle(DeleteTopicCommand request, CancellationToken ct)
    {
        var topic = await topics.GetByIdAsync(ForumTopicId.From(request.TopicId), ct);
        if (topic is null)
            return Error.NotFound(nameof(ForumTopic));

        var isModerator = currentUser.Role is UserRole.Moderator or UserRole.Admin;
        if (topic.AuthorId != currentUser.Id && !isModerator)
            return Error.Forbidden();

        topics.Remove(topic);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
