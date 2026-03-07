namespace BuildingBlocks.Application.Contracts;

public sealed record CustomerOrderExportRecord(
    Guid OrderId,
    string Status,
    string PaymentStatus,
    string FulfillmentStatus,
    decimal TotalAmount,
    string Currency,
    DateTime PlacedAtUtc,
    string ShippingMethodName);