using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

public interface IJournalEntryRepository
{
    Task<IReadOnlyList<JournalEntry>> GetBySessionAsync(GameSessionId sessionId, CancellationToken ct = default);
    Task<JournalEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(JournalEntry entry, CancellationToken ct = default);
    void Update(JournalEntry entry);
    void Remove(JournalEntry entry);
}
