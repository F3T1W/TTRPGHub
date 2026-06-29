using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Sessions.Commands.JoinSession;

public sealed record JoinSessionCommand(Guid SessionId) : IRequest<Result>;
