namespace Shipping.Application.Providers;

public sealed record CarrierCreateLabelRequest(
    Guid ShipmentId,
    string? TrackingNumber,
    string? TrackingUrl);
