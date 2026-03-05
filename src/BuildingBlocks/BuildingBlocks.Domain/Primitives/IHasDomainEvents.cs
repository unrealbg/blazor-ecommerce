namespace BuildingBlocks.Domain.Primitives;

public interface IHasDomainEvents
{
    IReadOnlyCollection<DomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
