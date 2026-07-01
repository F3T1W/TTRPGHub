namespace TTRPGHub.Entities.Discussions;

public readonly record struct DiscussionPostId(Guid Value)
{
    public static DiscussionPostId New() => new(Guid.NewGuid());
    public static DiscussionPostId From(Guid v) => new(v);
}
