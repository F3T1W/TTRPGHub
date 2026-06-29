using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Sessions.Commands.CreateSession;

public sealed record CreateSessionCommand(
    string Title,
    string? Description,
    string System,
    int MaxPlayers,
    DateTime ScheduledAt
) : IRequest<Result<CreateSessionResponse>>;

public sealed record CreateSessionResponse(Guid SessionId, string Title);
