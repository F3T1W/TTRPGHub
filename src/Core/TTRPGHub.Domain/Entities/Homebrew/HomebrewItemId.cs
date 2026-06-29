namespace TTRPGHub.Entities.Homebrew;

public readonly record struct HomebrewItemId(Guid Value)
{
    public static HomebrewItemId New() => new(Guid.NewGuid());
    public static HomebrewItemId From(Guid value) => new(value);
}
