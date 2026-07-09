using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities.Forum;
using TTRPGHub.Entities.Moderation;
using TTRPGHub.Repositories;
using TTRPGHub.Repositories.Forum;

namespace TTRPGHub.Features.Forum.Commands.SetTopicPinned;

internal sealed class SetTopicPinnedCommandHandler(
    IForumTopicRepository topics,
    IModerationLogRepository moderationLog,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork
) : IRequestHandler<SetTopicPinnedCommand, Result>
{
    public async Task<Result> Handle(SetTopicPinnedCommand request, CancellationToken ct)
    {
        var topic = await topics.GetByIdAsync(ForumTopicId.From(request.TopicId), ct);
        if (topic is null)
            return Error.NotFound(nameof(ForumTopic));

        if (request.Pinned) topic.Pin(); else topic.Unpin();

        await moderationLog.AddAsync(ModerationLogEntry.Create(
            currentUser.Id, request.Pinned ? "PinTopic" : "UnpinTopic", nameof(ForumTopic), topic.Id.Value), ct);

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
