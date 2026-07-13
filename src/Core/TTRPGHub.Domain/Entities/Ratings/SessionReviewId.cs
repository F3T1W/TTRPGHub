namespace TTRPGHub.Entities.Ratings;

public readonly record struct SessionReviewId(Guid Value)
{
    public static SessionReviewId New() => new(Guid.NewGuid());
    public static SessionReviewId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
