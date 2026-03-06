namespace Shipping.Application.Providers;

public sealed record CarrierTrackingRequest(
    Guid ShipmentId,
    string? TrackingNumber,
    string? TrackingUrl);
