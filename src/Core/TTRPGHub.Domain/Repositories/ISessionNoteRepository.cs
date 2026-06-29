using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

public interface ISessionNoteRepository
{
    Task<SessionNote?> GetByIdAsync(SessionNoteId id, CancellationToken ct = default);
    Task<IReadOnlyList<SessionNote>> GetByCampaignAsync(CampaignId campaignId, CancellationToken ct = default);
    Task AddAsync(SessionNote note, CancellationToken ct = default);
    void Update(SessionNote note);
    void Delete(SessionNote note);
}
