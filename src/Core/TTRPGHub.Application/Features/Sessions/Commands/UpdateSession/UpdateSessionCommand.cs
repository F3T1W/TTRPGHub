using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Sessions.Commands.UpdateSession;

public sealed record UpdateSessionCommand(
    Guid SessionId,
    string Title,
    string? Description,
    string System,
    int MaxPlayers,
    DateTime ScheduledAt
) : IRequest<Result>;
