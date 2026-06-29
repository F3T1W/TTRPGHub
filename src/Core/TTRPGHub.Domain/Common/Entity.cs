namespace TTRPGHub.Common;

public abstract class Entity<TId> : IHasDomainEvents where TId : notnull
{
    public TId Id { get; protected init; } = default!;

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        return ReferenceEquals(this, other) || Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();
}
