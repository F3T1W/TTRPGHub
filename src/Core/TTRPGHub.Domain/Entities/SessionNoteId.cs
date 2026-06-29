namespace TTRPGHub.Entities;

public readonly record struct SessionNoteId(Guid Value)
{
    public static SessionNoteId New() => new(Guid.NewGuid());
    public static SessionNoteId Empty => new(Guid.Empty);
}
