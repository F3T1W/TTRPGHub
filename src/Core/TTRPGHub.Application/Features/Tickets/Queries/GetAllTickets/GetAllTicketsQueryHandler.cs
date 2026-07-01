using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.Tickets.Queries.GetMyTickets;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Tickets.Queries.GetAllTickets;

internal sealed class GetAllTicketsQueryHandler(ISupportTicketRepository tickets)
    : IRequestHandler<GetAllTicketsQuery, Result<List<TicketDto>>>
{
    public async Task<Result<List<TicketDto>>> Handle(GetAllTicketsQuery request, CancellationToken ct)
    {
        var items = await tickets.GetAllAsync(ct);
        return items.Select(GetMyTicketsQueryHandler.Map).ToList();
    }
}
