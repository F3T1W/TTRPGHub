using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities.Pf2e;
using TTRPGHub.Repositories.Pf2e;

namespace TTRPGHub.Repositories.Pf2e;

internal sealed class Pf2eVehicleRepository(AppDbContext db) : IPf2eVehicleRepository
{
    public async Task<(IReadOnlyList<Pf2eVehicle> Items, int Total)> SearchAsync(
        string? search, int? level, int page, int pageSize, CancellationToken ct = default)
    {
        var q = db.Pf2eVehicles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(v => v.NameRu.ToLower().Contains(search.ToLower()) || v.Name.ToLower().Contains(search.ToLower()));

        if (level.HasValue)
            q = q.Where(v => v.Level == level.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(v => v.Level).ThenBy(v => v.NameRu)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items, total);
    }

    public Task<Pf2eVehicle?> GetByIdAsync(Pf2eVehicleId id, CancellationToken ct = default) =>
        db.Pf2eVehicles.FirstOrDefaultAsync(v => v.Id == id, ct);

    public Task<bool> AnyAsync(CancellationToken ct = default) =>
        db.Pf2eVehicles.AnyAsync(ct);

    public async Task AddRangeAsync(IEnumerable<Pf2eVehicle> vehicles, CancellationToken ct = default) =>
        await db.Pf2eVehicles.AddRangeAsync(vehicles, ct);
}
