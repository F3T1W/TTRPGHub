using TTRPGHub.Entities.Forum;

namespace TTRPGHub.Repositories.Forum;

public interface IForumTopicRepository
{
    Task<(List<ForumTopic> Items, int Total)> GetByCategoryAsync(
        ForumCategoryId categoryId, int page, int pageSize, CancellationToken ct = default);
    Task<ForumTopic?> GetByIdAsync(ForumTopicId id, CancellationToken ct = default);
    void Add(ForumTopic topic);
    void Remove(ForumTopic topic);
}
