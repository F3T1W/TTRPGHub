namespace TTRPGHub.Entities.Forum;

public readonly record struct ForumCategoryId(Guid Value)
{
    public static ForumCategoryId New() => new(Guid.NewGuid());
    public static ForumCategoryId From(Guid value) => new(value);
}
