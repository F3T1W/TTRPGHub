using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

public interface ITicketCommentRepository
{
    Task<List<TicketComment>> GetByTicketAsync(SupportTicketId ticketId, CancellationToken ct = default);
    Task AddAsync(TicketComment comment, CancellationToken ct = default);
}
