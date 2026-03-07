namespace Cart.Application.Carts;

public sealed record CartLineDto(
    Guid ProductId,
    Guid VariantId,
    string? Sku,
    string ProductName,
    string? VariantName,
    string? SelectedOptionsJson,
    string? ImageUrl,
    string Currency,
    decimal UnitAmount,
    int Quantity);
