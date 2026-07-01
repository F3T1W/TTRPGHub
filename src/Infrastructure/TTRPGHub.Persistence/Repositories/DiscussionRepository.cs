using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Discussions;
using TTRPGHub.Repositories;

namespace TTRPGHub.Persistence.Repositories;

internal sealed class DiscussionRepository(AppDbContext db) : IDiscussionRepository
{
    public async Task<IReadOnlyList<DiscussionPost>> GetByEntityAsync(
        DiscussionEntityType type, string slug, CancellationToken ct = default)
    {
        return await db.DiscussionPosts
            .Include(p => p.Author)
            .Include(p => p.Likes)
            .Where(p => p.EntityType == type && p.EntitySlug == slug.ToLowerInvariant())
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public Task<DiscussionPost?> GetByIdAsync(DiscussionPostId id, CancellationToken ct = default) =>
        db.DiscussionPosts
            .Include(p => p.Likes)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> HasLikedAsync(DiscussionPostId postId, UserId userId, CancellationToken ct = default) =>
        db.DiscussionLikes.AnyAsync(l => l.PostId == postId && l.UserId == userId, ct);

    public async Task AddAsync(DiscussionPost post, CancellationToken ct = default) =>
        await db.DiscussionPosts.AddAsync(post, ct);

    public void Remove(DiscussionPost post) => db.DiscussionPosts.Remove(post);

    public async Task AddLikeAsync(DiscussionLike like, CancellationToken ct = default) =>
        await db.DiscussionLikes.AddAsync(like, ct);

    public void RemoveLike(DiscussionLike like) => db.DiscussionLikes.Remove(like);

    public Task<DiscussionLike?> GetLikeAsync(DiscussionPostId postId, UserId userId, CancellationToken ct = default) =>
        db.DiscussionLikes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId, ct);
}
