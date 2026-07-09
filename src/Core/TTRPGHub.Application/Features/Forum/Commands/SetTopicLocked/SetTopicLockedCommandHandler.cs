using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities.Forum;
using TTRPGHub.Entities.Moderation;
using TTRPGHub.Repositories;
using TTRPGHub.Repositories.Forum;

namespace TTRPGHub.Features.Forum.Commands.SetTopicLocked;

internal sealed class SetTopicLockedCommandHandler(
    IForumTopicRepository topics,
    IModerationLogRepository moderationLog,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork
) : IRequestHandler<SetTopicLockedCommand, Result>
{
    public async Task<Result> Handle(SetTopicLockedCommand request, CancellationToken ct)
    {
        var topic = await topics.GetByIdAsync(ForumTopicId.From(request.TopicId), ct);
        if (topic is null)
            return Error.NotFound(nameof(ForumTopic));

        if (request.Locked) topic.Lock(); else topic.Unlock();

        await moderationLog.AddAsync(ModerationLogEntry.Create(
            currentUser.Id, request.Locked ? "LockTopic" : "UnlockTopic", nameof(ForumTopic), topic.Id.Value), ct);

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
