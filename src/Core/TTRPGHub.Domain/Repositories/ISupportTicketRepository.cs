using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

public interface ISupportTicketRepository
{
    Task<SupportTicket?> GetByIdAsync(SupportTicketId id, CancellationToken ct = default);
    Task<(IReadOnlyList<SupportTicket> Items, int Total)> GetByReporterAsync(
        UserId reporterId, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<SupportTicket>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(SupportTicket ticket, CancellationToken ct = default);
    void Update(SupportTicket ticket);
}
