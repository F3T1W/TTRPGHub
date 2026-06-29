using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Homebrew;
using TTRPGHub.Repositories;

namespace TTRPGHub.Persistence.Repositories;

internal sealed class HomebrewRepository(AppDbContext db) : IHomebrewRepository
{
    public async Task<(List<HomebrewItem> Items, int Total)> SearchAsync(
        string? query, string? system, HomebrewType? type, string? tag,
        int page, int pageSize, CancellationToken ct)
    {
        var q = db.HomebrewItems
            .Include(i => i.Author)
            .Include(i => i.Likes)
            .Where(i => i.IsPublished);

        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(i => i.Title.Contains(query) || i.Description.Contains(query));

        if (!string.IsNullOrWhiteSpace(system))
            q = q.Where(i => i.System == system);

        if (type.HasValue)
            q = q.Where(i => i.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(tag))
            q = q.Where(i => i.Tags.Contains(tag));

        q = q.OrderByDescending(i => i.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task<HomebrewItem?> GetByIdAsync(HomebrewItemId id, CancellationToken ct) =>
        db.HomebrewItems
            .Include(i => i.Author)
            .Include(i => i.Likes)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<List<HomebrewItem>> GetByAuthorAsync(UserId authorId, CancellationToken ct) =>
        db.HomebrewItems
            .Include(i => i.Likes)
            .Where(i => i.AuthorId == authorId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

    public Task<bool> HasLikeAsync(HomebrewItemId itemId, UserId userId, CancellationToken ct) =>
        db.HomebrewLikes.AnyAsync(l => l.ItemId == itemId && l.UserId == userId, ct);

    public void Add(HomebrewItem item) => db.HomebrewItems.Add(item);
    public void Remove(HomebrewItem item) => db.HomebrewItems.Remove(item);
    public void AddLike(HomebrewLike like) => db.HomebrewLikes.Add(like);
    public void RemoveLike(HomebrewLike like) => db.HomebrewLikes.Remove(like);
}
