namespace Inventory.Application.Stock;

public sealed record StockItemSummaryDto(
    Guid Id,
    Guid ProductId,
    string? Sku,
    int OnHandQuantity,
    int ReservedQuantity,
    int AvailableQuantity,
    bool IsTracked,
    bool AllowBackorder,
    bool IsInStock,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
