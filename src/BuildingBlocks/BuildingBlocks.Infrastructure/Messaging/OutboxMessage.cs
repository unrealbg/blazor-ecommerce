using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Infrastructure.Messaging;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    public Guid Id { get; private set; }

    public DateTime OccurredOnUtc { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public DateTime? ProcessedOnUtc { get; private set; }

    public string? Error { get; private set; }

    public static OutboxMessage Create(DomainEvent domainEvent, IEventSerializer eventSerializer)
    {
        var serialized = eventSerializer.Serialize(domainEvent);

        return new OutboxMessage
        {
            Id = domainEvent.EventId,
            OccurredOnUtc = domainEvent.OccurredOnUtc,
            Type = serialized.Type,
            Payload = serialized.Payload,
        };
    }

    public void MarkProcessed(DateTime processedOnUtc)
    {
        ProcessedOnUtc = processedOnUtc;
        Error = null;
    }

    public void MarkFailed(string error, DateTime processedOnUtc)
    {
        Error = error;
        ProcessedOnUtc = processedOnUtc;
    }
}
