namespace Orders.Application.Orders;

public sealed record OrderDto(
    Guid Id,
    string CustomerId,
    string Currency,
    decimal SubtotalAmount,
    decimal ShippingPriceAmount,
    string ShippingCurrency,
    string ShippingMethodCode,
    string ShippingMethodName,
    decimal TotalAmount,
    string Status,
    string FulfillmentStatus,
    DateTime PlacedAtUtc,
    OrderAddressDto ShippingAddress,
    OrderAddressDto BillingAddress,
    IReadOnlyCollection<OrderLineDto> Lines);
