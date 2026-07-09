using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Tickets.Queries.GetMyTickets;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Tickets.Queries.GetTicketById;

internal sealed class GetTicketByIdQueryHandler(
    ISupportTicketRepository tickets,
    ICurrentUser currentUser
) : IRequestHandler<GetTicketByIdQuery, Result<TicketDto>>
{
    public async Task<Result<TicketDto>> Handle(GetTicketByIdQuery request, CancellationToken ct)
    {
        var ticket = await tickets.GetByIdAsync(new SupportTicketId(request.TicketId), ct);
        if (ticket is null)
            return Error.NotFound(nameof(SupportTicket));

        var isModerator = currentUser.Role is UserRole.Moderator or UserRole.Admin;
        if (ticket.ReporterId != currentUser.Id && !isModerator)
            return Error.Forbidden();

        return new TicketDto(
            ticket.Id.Value, ticket.Title, ticket.Description, ticket.ContactInfo,
            ticket.Status.ToString(), ticket.CreatedAt, ticket.UpdatedAt,
            ticket.Attachments.Select(a => new TicketAttachmentDto(a.Id, a.Url, a.FileName, a.ContentType)).ToList());
    }
}
