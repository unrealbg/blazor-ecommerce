namespace BuildingBlocks.Application.Contracts;

public sealed record InventoryAvailabilitySnapshot(
    Guid ProductId,
    Guid? VariantId,
    string? Sku,
    bool IsTracked,
    bool AllowBackorder,
    int OnHandQuantity,
    int ReservedQuantity,
    int AvailableQuantity,
    bool IsInStock);
