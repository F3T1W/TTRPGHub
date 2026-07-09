using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Forum.Commands.SetTopicLocked;

public sealed record SetTopicLockedCommand(Guid TopicId, bool Locked) : IRequest<Result>;
