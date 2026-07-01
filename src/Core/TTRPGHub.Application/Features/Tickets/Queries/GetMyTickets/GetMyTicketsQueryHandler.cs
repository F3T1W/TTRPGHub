using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Tickets.Queries.GetMyTickets;

internal sealed class GetMyTicketsQueryHandler(
    ISupportTicketRepository tickets,
    ICurrentUser currentUser
) : IRequestHandler<GetMyTicketsQuery, Result<PagedTicketsResult>>
{
    public async Task<Result<PagedTicketsResult>> Handle(GetMyTicketsQuery request, CancellationToken ct)
    {
        var (items, total) = await tickets.GetByReporterAsync(currentUser.Id, request.Page, request.PageSize, ct);

        var dtos = items.Select(Map).ToList();
        return new PagedTicketsResult(dtos, total, request.Page, request.PageSize);
    }

    internal static TicketDto Map(TTRPGHub.Entities.SupportTicket t) => new(
        t.Id.Value, t.Title, t.Description, t.ContactInfo,
        t.Status.ToString(), t.CreatedAt, t.UpdatedAt,
        t.Attachments.Select(a => new TicketAttachmentDto(a.Id, a.Url, a.FileName, a.ContentType)).ToList());
}
