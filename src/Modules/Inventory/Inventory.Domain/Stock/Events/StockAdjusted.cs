using BuildingBlocks.Domain.Primitives;

namespace Inventory.Domain.Stock.Events;

public sealed record StockAdjusted(
    Guid ProductId,
    string? Sku,
    int QuantityDelta,
    string? Reason) : DomainEvent;
