namespace Storefront.Web.Services.Api;

public sealed record StoreShipmentEvent(
    Guid Id,
    string EventType,
    string? Message,
    string? ExternalEventId,
    DateTime OccurredAtUtc,
    string? MetadataJson);
