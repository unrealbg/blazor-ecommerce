namespace Inventory.Application.Stock;

public sealed record StockMovementDto(
    Guid Id,
    Guid ProductId,
    string? Sku,
    string Type,
    int QuantityDelta,
    Guid? ReferenceId,
    string? Reason,
    DateTime CreatedAtUtc,
    string? CreatedBy);
