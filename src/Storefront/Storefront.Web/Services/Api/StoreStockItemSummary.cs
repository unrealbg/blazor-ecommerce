namespace Storefront.Web.Services.Api;

public sealed record StoreStockItemSummary(
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
