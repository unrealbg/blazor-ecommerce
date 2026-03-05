using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Infrastructure.Messaging;

public interface IEventSerializer
{
    SerializedDomainEvent Serialize(DomainEvent domainEvent);

    DomainEvent Deserialize(string eventType, string payload);
}
