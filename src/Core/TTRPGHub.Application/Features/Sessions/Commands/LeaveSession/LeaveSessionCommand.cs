using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Sessions.Commands.LeaveSession;

public sealed record LeaveSessionCommand(Guid SessionId) : IRequest<Result>;
