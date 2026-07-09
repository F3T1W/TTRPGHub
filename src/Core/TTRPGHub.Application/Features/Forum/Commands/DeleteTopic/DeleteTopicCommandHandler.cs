using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Forum;
using TTRPGHub.Entities.Moderation;
using TTRPGHub.Repositories;
using TTRPGHub.Repositories.Forum;

namespace TTRPGHub.Features.Forum.Commands.DeleteTopic;

internal sealed class DeleteTopicCommandHandler(
    IForumTopicRepository topics,
    IModerationLogRepository moderationLog,
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

        if (isModerator && topic.AuthorId != currentUser.Id)
        {
            await moderationLog.AddAsync(ModerationLogEntry.Create(
                currentUser.Id, "DeleteTopic", nameof(ForumTopic), topic.Id.Value, topic.Title), ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
