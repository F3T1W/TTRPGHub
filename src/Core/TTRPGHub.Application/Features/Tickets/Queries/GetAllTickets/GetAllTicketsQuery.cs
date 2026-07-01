using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.Tickets.Queries.GetMyTickets;

namespace TTRPGHub.Features.Tickets.Queries.GetAllTickets;

public sealed record GetAllTicketsQuery : IRequest<Result<List<TicketDto>>>;
