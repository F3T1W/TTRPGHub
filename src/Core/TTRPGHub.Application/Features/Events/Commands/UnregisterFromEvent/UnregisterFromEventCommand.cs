using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Events.Commands.UnregisterFromEvent;

public sealed record UnregisterFromEventCommand(Guid EventId) : IRequest<Result>;
