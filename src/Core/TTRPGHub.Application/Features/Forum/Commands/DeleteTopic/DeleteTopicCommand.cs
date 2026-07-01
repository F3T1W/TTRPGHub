using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Forum.Commands.DeleteTopic;

public sealed record DeleteTopicCommand(Guid TopicId) : IRequest<Result>;
