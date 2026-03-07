namespace Orders.Application.Orders;

public sealed record OrderDto(
    Guid Id,
    string CustomerId,
    string Currency,
    decimal SubtotalBeforeDiscountAmount,
    decimal SubtotalAmount,
    decimal LineDiscountTotalAmount,
    decimal CartDiscountTotalAmount,
    decimal ShippingPriceAmount,
    decimal ShippingDiscountTotalAmount,
    string ShippingCurrency,
    string ShippingMethodCode,
    string ShippingMethodName,
    decimal TotalAmount,
    string? AppliedCouponsJson,
    string? AppliedPromotionsJson,
    string Status,
    string FulfillmentStatus,
    DateTime PlacedAtUtc,
    OrderAddressDto ShippingAddress,
    OrderAddressDto BillingAddress,
    IReadOnlyCollection<OrderLineDto> Lines);
