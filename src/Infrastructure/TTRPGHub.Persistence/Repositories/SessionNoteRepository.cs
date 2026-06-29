using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Repositories;

internal sealed class SessionNoteRepository(AppDbContext db) : ISessionNoteRepository
{
    public Task<SessionNote?> GetByIdAsync(SessionNoteId id, CancellationToken ct = default) =>
        db.SessionNotes.FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<IReadOnlyList<SessionNote>> GetByCampaignAsync(CampaignId campaignId, CancellationToken ct = default) =>
        await db.SessionNotes
            .Where(n => n.CampaignId == campaignId)
            .OrderByDescending(n => n.SessionDate)
            .ToListAsync(ct);

    public async Task AddAsync(SessionNote note, CancellationToken ct = default) =>
        await db.SessionNotes.AddAsync(note, ct);

    public void Update(SessionNote note) => db.SessionNotes.Update(note);

    public void Delete(SessionNote note) => db.SessionNotes.Remove(note);
}
