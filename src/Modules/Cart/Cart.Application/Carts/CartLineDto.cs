namespace Cart.Application.Carts;

public sealed record CartLineDto(
    Guid ProductId,
    string Name,
    string Currency,
    decimal UnitAmount,
    int Quantity);
