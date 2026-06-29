using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities.Forum;
using TTRPGHub.Repositories.Forum;

namespace TTRPGHub.Persistence.Repositories.Forum;

internal sealed class ForumCategoryRepository(AppDbContext db) : IForumCategoryRepository
{
    public Task<List<ForumCategory>> GetAllAsync(CancellationToken ct) =>
        db.ForumCategories.Include(c => c.Topics).OrderBy(c => c.DisplayOrder).ToListAsync(ct);

    public Task<ForumCategory?> GetByIdAsync(ForumCategoryId id, CancellationToken ct) =>
        db.ForumCategories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<ForumCategory?> GetBySlugAsync(string slug, CancellationToken ct) =>
        db.ForumCategories.FirstOrDefaultAsync(c => c.Slug == slug, ct);

    public void Add(ForumCategory category) => db.ForumCategories.Add(category);
}
