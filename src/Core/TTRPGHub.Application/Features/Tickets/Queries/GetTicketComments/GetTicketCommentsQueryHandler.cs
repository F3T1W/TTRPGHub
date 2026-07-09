using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Tickets.Queries.GetTicketComments;

internal sealed class GetTicketCommentsQueryHandler(
    ISupportTicketRepository tickets,
    ITicketCommentRepository comments,
    IUserRepository users,
    ICurrentUser currentUser
) : IRequestHandler<GetTicketCommentsQuery, Result<List<TicketCommentDto>>>
{
    public async Task<Result<List<TicketCommentDto>>> Handle(GetTicketCommentsQuery request, CancellationToken ct)
    {
        var ticketId = new SupportTicketId(request.TicketId);
        var ticket = await tickets.GetByIdAsync(ticketId, ct);
        if (ticket is null)
            return Error.NotFound(nameof(SupportTicket));

        var isModerator = currentUser.Role is UserRole.Moderator or UserRole.Admin;
        if (ticket.ReporterId != currentUser.Id && !isModerator)
            return Error.Forbidden();

        var list = await comments.GetByTicketAsync(ticketId, ct);
        var result = new List<TicketCommentDto>(list.Count);

        foreach (var c in list)
        {
            var author = await users.GetByIdAsync(c.AuthorId, ct);
            result.Add(new TicketCommentDto(c.Id.Value, c.TicketId.Value, c.AuthorId.Value, author?.Username ?? "—", c.Body, c.CreatedAt));
        }

        return result;
    }
}
