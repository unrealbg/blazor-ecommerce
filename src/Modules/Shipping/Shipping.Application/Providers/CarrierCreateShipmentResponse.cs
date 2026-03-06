namespace Shipping.Application.Providers;

public sealed record CarrierCreateShipmentResponse(
    string CarrierName,
    string? CarrierServiceCode,
    string TrackingNumber,
    string? TrackingUrl,
    string? LabelReference);
