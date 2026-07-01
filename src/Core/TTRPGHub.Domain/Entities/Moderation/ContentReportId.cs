namespace TTRPGHub.Entities.Moderation;

public readonly record struct ContentReportId(Guid Value)
{
    public static ContentReportId New() => new(Guid.NewGuid());
    public static ContentReportId From(Guid value) => new(value);
}
