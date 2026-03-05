namespace Orders.Application.Orders;

public sealed record CheckoutIdempotencyRecord(
    string IdempotencyKey,
    string CustomerId,
    Guid OrderId);
