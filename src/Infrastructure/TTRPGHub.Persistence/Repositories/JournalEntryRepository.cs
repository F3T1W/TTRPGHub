using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Repositories;

internal sealed class JournalEntryRepository(AppDbContext db) : IJournalEntryRepository
{
    public async Task<IReadOnlyList<JournalEntry>> GetBySessionAsync(GameSessionId sessionId, CancellationToken ct = default)
    {
        var list = await db.JournalEntries
            .Where(e => e.SessionId == sessionId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);
        return list.AsReadOnly();
    }

    public Task<JournalEntry?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.JournalEntries.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task AddAsync(JournalEntry entry, CancellationToken ct = default) =>
        await db.JournalEntries.AddAsync(entry, ct);

    public void Update(JournalEntry entry) => db.JournalEntries.Update(entry);

    public void Remove(JournalEntry entry) => db.JournalEntries.Remove(entry);
}
