using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

internal sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Email.Value == email, ct);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        db.Users.AnyAsync(u => u.Email.Value == email, ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await db.Users.AddAsync(user, ct);

    public void Update(User user) =>
        db.Users.Update(user);

    public async Task<(IReadOnlyList<User> Items, int Total)> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u =>
                EF.Functions.ILike(u.Username, $"%{search}%") || EF.Functions.ILike(u.Email.Value, $"%{search}%"));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
