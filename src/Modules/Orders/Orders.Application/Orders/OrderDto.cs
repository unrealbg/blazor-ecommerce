namespace Orders.Application.Orders;

public sealed record OrderDto(
    Guid Id,
    Guid CartId,
    Guid CustomerId,
    string Currency,
    decimal TotalAmount,
    string Status,
    DateTime CreatedOnUtc);
