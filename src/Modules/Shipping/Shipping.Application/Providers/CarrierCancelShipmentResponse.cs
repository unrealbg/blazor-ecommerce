namespace Shipping.Application.Providers;

public sealed record CarrierCancelShipmentResponse(bool Cancelled, string? Message);
