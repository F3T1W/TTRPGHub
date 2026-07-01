using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Repositories;

internal sealed class SupportTicketRepository(AppDbContext db) : ISupportTicketRepository
{
    public Task<SupportTicket?> GetByIdAsync(SupportTicketId id, CancellationToken ct = default) =>
        db.SupportTickets.Include(t => t.Attachments).FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<(IReadOnlyList<SupportTicket> Items, int Total)> GetByReporterAsync(
        UserId reporterId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.SupportTickets
            .Include(t => t.Attachments)
            .Where(t => t.ReporterId == reporterId)
            .OrderByDescending(t => t.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<IReadOnlyList<SupportTicket>> GetAllAsync(CancellationToken ct = default) =>
        await db.SupportTickets
            .Include(t => t.Attachments)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(SupportTicket ticket, CancellationToken ct = default) =>
        await db.SupportTickets.AddAsync(ticket, ct);

    public void Update(SupportTicket ticket) =>
        db.SupportTickets.Update(ticket);
}
