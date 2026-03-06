using BuildingBlocks.Domain.Primitives;

namespace Inventory.Domain.Stock.Events;

public sealed record StockAvailabilityChanged(
    Guid ProductId,
    string? Sku,
    bool IsTracked,
    bool AllowBackorder,
    int AvailableQuantity,
    bool IsInStock) : DomainEvent;
