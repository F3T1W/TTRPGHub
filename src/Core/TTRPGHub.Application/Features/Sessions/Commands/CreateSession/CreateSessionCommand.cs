using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Features.Sessions.Commands.CreateSession;

public sealed record CreateSessionCommand(
    string Title,
    string? Description,
    string System,
    int MaxPlayers,
    DateTime ScheduledAt,
    SessionFormat Format,
    string? Location
) : IRequest<Result<CreateSessionResponse>>;

public sealed record CreateSessionResponse(Guid SessionId, string Title);
