using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Abstractions;

public interface IDomainEventHandler<in TDomainEvent> where TDomainEvent : DomainEvent
{
    Task Handle(TDomainEvent domainEvent, CancellationToken cancellationToken);
}
