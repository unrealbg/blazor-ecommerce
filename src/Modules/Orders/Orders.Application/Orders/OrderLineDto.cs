namespace Orders.Application.Orders;

public sealed record OrderLineDto(
    Guid ProductId,
    string Name,
    string Currency,
    decimal UnitAmount,
    int Quantity);
