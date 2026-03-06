namespace BuildingBlocks.Application.Contracts;

public sealed record OrderFulfillmentSnapshot(
    Guid OrderId,
    string CustomerId,
    string Status,
    string FulfillmentStatus,
    string ShippingMethodCode,
    string ShippingMethodName,
    decimal ShippingPriceAmount,
    string ShippingCurrency,
    decimal TotalAmount,
    string Currency,
    OrderFulfillmentAddressSnapshot ShippingAddress);
