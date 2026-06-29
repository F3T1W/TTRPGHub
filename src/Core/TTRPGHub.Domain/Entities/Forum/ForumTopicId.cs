namespace TTRPGHub.Entities.Forum;

public readonly record struct ForumTopicId(Guid Value)
{
    public static ForumTopicId New() => new(Guid.NewGuid());
    public static ForumTopicId From(Guid value) => new(value);
}
