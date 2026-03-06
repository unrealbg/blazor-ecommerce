namespace Orders.Application.Orders;

public sealed record OrderDto(
    Guid Id,
    string CustomerId,
    string Currency,
    decimal SubtotalAmount,
    decimal TotalAmount,
    string Status,
    DateTime PlacedAtUtc,
    OrderAddressDto ShippingAddress,
    OrderAddressDto BillingAddress,
    IReadOnlyCollection<OrderLineDto> Lines);
