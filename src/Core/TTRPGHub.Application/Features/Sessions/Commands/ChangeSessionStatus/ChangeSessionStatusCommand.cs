using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Features.Sessions.Commands.ChangeSessionStatus;

public sealed record ChangeSessionStatusCommand(Guid SessionId, SessionStatus NewStatus) : IRequest<Result>;
