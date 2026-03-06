namespace Shipping.Application.Providers;

public sealed record CarrierWebhookParseResult(
    string ExternalEventId,
    string EventType,
    Guid? ShipmentId,
    string? TrackingNumber,
    string? Status,
    string? Message,
    string? MetadataJson);
