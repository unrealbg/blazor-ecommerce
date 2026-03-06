namespace Shipping.Application.Shipping;

public sealed record ShipmentEventDto(
    Guid Id,
    string EventType,
    string? Message,
    string? ExternalEventId,
    DateTime OccurredAtUtc,
    string? MetadataJson);
