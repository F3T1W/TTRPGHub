using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Repositories;

internal sealed class UserCalendarPreferenceRepository(AppDbContext db) : IUserCalendarPreferenceRepository
{
    public Task<UserCalendarPreference?> GetByUserIdAsync(UserId userId, CancellationToken ct = default) =>
        db.UserCalendarPreferences.FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public Task<UserCalendarPreference?> GetByTokenAsync(Guid token, CancellationToken ct = default) =>
        db.UserCalendarPreferences.FirstOrDefaultAsync(p => p.CalendarToken == token, ct);

    public async Task AddAsync(UserCalendarPreference preference, CancellationToken ct = default) =>
        await db.UserCalendarPreferences.AddAsync(preference, ct);

    public void Update(UserCalendarPreference preference) =>
        db.UserCalendarPreferences.Update(preference);
}
