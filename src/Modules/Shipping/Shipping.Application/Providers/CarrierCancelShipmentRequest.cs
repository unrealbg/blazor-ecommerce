namespace Shipping.Application.Providers;

public sealed record CarrierCancelShipmentRequest(
    Guid ShipmentId,
    string? TrackingNumber,
    string? Reason);
