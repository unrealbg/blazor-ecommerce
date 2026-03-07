namespace Backoffice.Application.Backoffice;

public sealed record BackofficeShipmentEventDto(
    Guid Id,
    string EventType,
    string? Message,
    string? ExternalEventId,
    DateTime OccurredAtUtc,
    string? MetadataJson);
