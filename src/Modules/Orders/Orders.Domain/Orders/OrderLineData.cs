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
    int Quantity);
