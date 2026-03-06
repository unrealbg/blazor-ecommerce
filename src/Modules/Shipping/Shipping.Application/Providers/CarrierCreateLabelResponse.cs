namespace Shipping.Application.Providers;

public sealed record CarrierCreateLabelResponse(
    string? LabelReference,
    string? TrackingUrl);
