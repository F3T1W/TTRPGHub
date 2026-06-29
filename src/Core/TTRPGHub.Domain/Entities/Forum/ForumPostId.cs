namespace TTRPGHub.Entities.Forum;

public readonly record struct ForumPostId(Guid Value)
{
    public static ForumPostId New() => new(Guid.NewGuid());
    public static ForumPostId From(Guid value) => new(value);
}
