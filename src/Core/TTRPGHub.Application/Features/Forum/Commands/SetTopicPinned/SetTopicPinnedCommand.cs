using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Forum.Commands.SetTopicPinned;

public sealed record SetTopicPinnedCommand(Guid TopicId, bool Pinned) : IRequest<Result>;
