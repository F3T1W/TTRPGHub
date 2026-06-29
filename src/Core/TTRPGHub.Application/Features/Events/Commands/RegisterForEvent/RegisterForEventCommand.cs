using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Events.Commands.RegisterForEvent;

public sealed record RegisterForEventCommand(Guid EventId) : IRequest<Result>;
