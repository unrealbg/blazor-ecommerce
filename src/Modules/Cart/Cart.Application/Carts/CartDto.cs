namespace Cart.Application.Carts;

public sealed record CartDto(
    Guid Id,
    string CustomerId,
    IReadOnlyCollection<CartLineDto> Lines,
    IReadOnlyCollection<string> Messages);
