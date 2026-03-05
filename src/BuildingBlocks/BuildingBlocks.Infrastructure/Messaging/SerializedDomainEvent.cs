using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Infrastructure.Messaging;

public sealed record SerializedDomainEvent(string Type, string Payload)
{
    public static SerializedDomainEvent FromDomainEvent(DomainEvent domainEvent, string payload)
    {
        return new SerializedDomainEvent(domainEvent.GetType().AssemblyQualifiedName!, payload);
    }
}
