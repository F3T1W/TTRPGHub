using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.Tickets.Queries.GetMyTickets;

namespace TTRPGHub.Features.Tickets.Queries.GetTicketById;

public sealed record GetTicketByIdQuery(Guid TicketId) : IRequest<Result<TicketDto>>;
