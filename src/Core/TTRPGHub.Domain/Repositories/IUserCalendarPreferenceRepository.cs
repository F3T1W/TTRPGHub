using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

public interface IUserCalendarPreferenceRepository
{
    Task<UserCalendarPreference?> GetByUserIdAsync(UserId userId, CancellationToken ct = default);
    Task<UserCalendarPreference?> GetByTokenAsync(Guid token, CancellationToken ct = default);
    Task AddAsync(UserCalendarPreference preference, CancellationToken ct = default);
    void Update(UserCalendarPreference preference);
}
