using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Tickets.Commands.ChangeTicketStatus;

public sealed record ChangeTicketStatusCommand(Guid TicketId, string Status) : IRequest<Result>;
