namespace Storefront.Web.Services.Api;

public sealed record StoreShipment(
    Guid Id,
    Guid OrderId,
    Guid ShippingMethodId,
    string CarrierName,
    string? CarrierServiceCode,
    string? TrackingNumber,
    string? TrackingUrl,
    string Status,
    string RecipientName,
    string? RecipientPhone,
    string AddressSnapshotJson,
    decimal ShippingPriceAmount,
    string Currency,
    DateTime? ShippedAtUtc,
    DateTime? DeliveredAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyCollection<StoreShipmentEvent> Events);
