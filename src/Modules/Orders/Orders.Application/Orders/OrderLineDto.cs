namespace Orders.Application.Orders;

public sealed record OrderLineDto(
    Guid ProductId,
    Guid VariantId,
    string? Sku,
    string Name,
    string? VariantName,
    string? SelectedOptionsJson,
    string Currency,
    decimal BaseUnitAmount,
    decimal FinalUnitAmount,
    decimal? CompareAtPriceAmount,
    decimal DiscountTotalAmount,
    string? AppliedDiscountsJson,
    int Quantity);
