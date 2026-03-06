using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;

namespace Shipping.Domain.Shipping;

public sealed class ShipmentEvent : Entity<Guid>
{
    private ShipmentEvent()
    {
    }

    private ShipmentEvent(
        Guid id,
        Guid shipmentId,
        ShipmentEventType eventType,
        string? message,
        string? externalEventId,
        DateTime occurredAtUtc,
        string? metadataJson)
    {
        Id = id;
        ShipmentId = shipmentId;
        EventType = eventType;
        Message = message;
        ExternalEventId = externalEventId;
        OccurredAtUtc = occurredAtUtc;
        MetadataJson = metadataJson;
    }

    public Guid ShipmentId { get; private set; }

    public ShipmentEventType EventType { get; private set; }

    public string? Message { get; private set; }

    public string? ExternalEventId { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public string? MetadataJson { get; private set; }

    public static Result<ShipmentEvent> Create(
        Guid shipmentId,
        ShipmentEventType eventType,
        string? message,
        string? externalEventId,
        DateTime occurredAtUtc,
        string? metadataJson)
    {
        if (shipmentId == Guid.Empty)
        {
            return Result<ShipmentEvent>.Failure(new Error(
                "shipping.shipment_event.shipment.required",
                "Shipment id is required."));
        }

        return Result<ShipmentEvent>.Success(new ShipmentEvent(
            Guid.NewGuid(),
            shipmentId,
            eventType,
            string.IsNullOrWhiteSpace(message) ? null : message.Trim(),
            string.IsNullOrWhiteSpace(externalEventId) ? null : externalEventId.Trim(),
            occurredAtUtc,
            string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson.Trim()));
    }
}
