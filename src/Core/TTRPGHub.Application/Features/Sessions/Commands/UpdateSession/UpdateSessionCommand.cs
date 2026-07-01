using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Features.Sessions.Commands.UpdateSession;

public sealed record UpdateSessionCommand(
    Guid SessionId,
    string Title,
    string? Description,
    string System,
    int MaxPlayers,
    DateTime ScheduledAt,
    SessionFormat Format,
    string? Location
) : IRequest<Result>;
