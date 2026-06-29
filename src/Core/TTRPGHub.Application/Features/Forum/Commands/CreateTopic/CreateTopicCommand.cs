using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Forum.Commands.CreateTopic;

public sealed record CreateTopicCommand(Guid CategoryId, string Title, string FirstPostContent)
    : IRequest<Result<Guid>>;
