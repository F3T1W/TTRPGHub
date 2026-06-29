using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Events.Commands.CancelEvent;

public sealed record CancelEventCommand(Guid EventId) : IRequest<Result>;
