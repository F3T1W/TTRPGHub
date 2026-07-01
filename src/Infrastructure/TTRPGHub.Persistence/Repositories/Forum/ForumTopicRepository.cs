using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities.Forum;
using TTRPGHub.Repositories.Forum;

namespace TTRPGHub.Persistence.Repositories.Forum;

internal sealed class ForumTopicRepository(AppDbContext db) : IForumTopicRepository
{
    public async Task<(List<ForumTopic> Items, int Total)> GetByCategoryAsync(
        ForumCategoryId categoryId, int page, int pageSize, CancellationToken ct)
    {
        var query = db.ForumTopics
            .Include(t => t.Author)
            .Include(t => t.Posts)
            .Where(t => t.CategoryId == categoryId)
            .OrderByDescending(t => t.IsPinned)
            .ThenByDescending(t => t.LastPostAt ?? t.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task<ForumTopic?> GetByIdAsync(ForumTopicId id, CancellationToken ct) =>
        db.ForumTopics
            .Include(t => t.Category)
            .Include(t => t.Author)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public void Add(ForumTopic topic) => db.ForumTopics.Add(topic);
    public void Remove(ForumTopic topic) => db.ForumTopics.Remove(topic);
}
