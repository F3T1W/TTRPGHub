namespace TTRPGHub.Entities.Ratings;

public readonly record struct UserRatingId(Guid Value)
{
    public static UserRatingId New() => new(Guid.NewGuid());
    public static UserRatingId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
