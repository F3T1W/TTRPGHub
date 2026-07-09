using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Persistence.Repositories;

internal sealed class TicketCommentRepository(AppDbContext db) : ITicketCommentRepository
{
    public async Task<List<TicketComment>> GetByTicketAsync(SupportTicketId ticketId, CancellationToken ct = default) =>
        await db.TicketComments
            .Where(c => c.TicketId == ticketId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(TicketComment comment, CancellationToken ct = default) =>
        await db.TicketComments.AddAsync(comment, ct);
}
