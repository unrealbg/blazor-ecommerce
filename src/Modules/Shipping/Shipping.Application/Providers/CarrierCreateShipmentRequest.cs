namespace Shipping.Application.Providers;

public sealed record CarrierCreateShipmentRequest(
    Guid ShipmentId,
    Guid OrderId,
    string ShippingMethodCode,
    string ShippingMethodName,
    string RecipientName,
    string? RecipientPhone,
    string AddressSnapshotJson,
    decimal ShippingPriceAmount,
    string Currency);
