namespace Shipping.Application.Providers;

public sealed record CarrierTrackingResponse(
    string Status,
    string? TrackingNumber,
    string? TrackingUrl,
    string? Message);
