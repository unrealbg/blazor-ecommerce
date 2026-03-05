using System.ComponentModel.DataAnnotations.Schema;

namespace BuildingBlocks.Domain.Primitives;

public interface IHasDomainEvents
{
    IReadOnlyCollection<DomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}

public abstract class Entity<TId> : IHasDomainEvents where TId : notnull
{
    private readonly List<DomainEvent> _domainEvents = [];

    public TId Id { get; protected set; } = default!;

    [NotMapped]
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
