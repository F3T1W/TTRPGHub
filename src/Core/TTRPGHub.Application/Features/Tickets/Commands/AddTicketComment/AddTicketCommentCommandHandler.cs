using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Tickets.Commands.AddTicketComment;

internal sealed class AddTicketCommentCommandHandler(
    ISupportTicketRepository tickets,
    ITicketCommentRepository comments,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork
) : IRequestHandler<AddTicketCommentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddTicketCommentCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
            return Error.Validation("Body", "Комментарий не может быть пустым.");

        var ticketId = new SupportTicketId(request.TicketId);
        var ticket = await tickets.GetByIdAsync(ticketId, ct);
        if (ticket is null)
            return Error.NotFound(nameof(SupportTicket));

        var isModerator = currentUser.Role is UserRole.Moderator or UserRole.Admin;
        if (ticket.ReporterId != currentUser.Id && !isModerator)
            return Error.Forbidden();

        var comment = TicketComment.Create(ticketId, currentUser.Id, request.Body);
        await comments.AddAsync(comment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return comment.Id.Value;
    }
}
