using BuildingBlocks.Domain.Shared;

namespace Orders.Domain.Orders;

public sealed record OrderLineData(
    Guid ProductId,
    Guid VariantId,
    string? Sku,
    string ProductName,
    string? VariantName,
    string? SelectedOptionsJson,
    Money UnitPrice,
    int Quantity,
    decimal? BaseUnitAmount = null,
    decimal? CompareAtPriceAmount = null,
    decimal DiscountTotalAmount = 0m,
    string? AppliedDiscountsJson = null);
