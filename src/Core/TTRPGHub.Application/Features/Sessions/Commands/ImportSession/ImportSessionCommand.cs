using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Sessions.Commands.ImportSession;

public sealed record ImportSessionCommand(
    string Title,
    string System,
    DateTime ScheduledAt,
    int MaxPlayers,
    string? Description = null
) : IRequest<Result<ImportSessionResponse>>;

public sealed record ImportSessionResponse(Guid SessionId, string Title);
