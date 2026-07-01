using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Tickets.Commands.ChangeTicketStatus;

internal sealed class ChangeTicketStatusCommandHandler(
    ISupportTicketRepository tickets,
    IUnitOfWork unitOfWork
) : IRequestHandler<ChangeTicketStatusCommand, Result>
{
    public async Task<Result> Handle(ChangeTicketStatusCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<TicketStatus>(request.Status, ignoreCase: true, out var status))
            return Error.Validation("Status", "Допустимые статусы: Open, InProgress, Done.");

        var ticket = await tickets.GetByIdAsync(new SupportTicketId(request.TicketId), ct);
        if (ticket is null)
            return Error.NotFound(nameof(SupportTicket));

        ticket.ChangeStatus(status);
        tickets.Update(ticket);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
