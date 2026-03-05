using System.Text.Json;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Infrastructure.Messaging;

public sealed class SystemTextJsonEventSerializer : IEventSerializer
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public SerializedDomainEvent Serialize(DomainEvent domainEvent)
    {
        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonSerializerOptions);
        return SerializedDomainEvent.FromDomainEvent(domainEvent, payload);
    }

    public DomainEvent Deserialize(string eventType, string payload)
    {
        var resolvedType = ResolveEventType(eventType);

        if (resolvedType is null)
        {
            throw new InvalidOperationException($"Could not resolve domain event type '{eventType}'.");
        }

        var deserialized = JsonSerializer.Deserialize(payload, resolvedType, JsonSerializerOptions);

        if (deserialized is not DomainEvent domainEvent)
        {
            throw new InvalidOperationException($"Could not deserialize payload to '{eventType}'.");
        }

        return domainEvent;
    }

    private static Type? ResolveEventType(string eventType)
    {
        var type = Type.GetType(eventType, throwOnError: false);

        if (type is not null)
        {
            return type;
        }

        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(assembly => assembly.GetType(eventType, throwOnError: false))
            .FirstOrDefault(candidate => candidate is not null);
    }
}
