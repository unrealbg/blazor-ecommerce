using BuildingBlocks.Domain.Primitives;

namespace Inventory.Domain.Stock.Events;

public sealed record StockRestored(
    Guid ProductId,
    string? Sku,
    Guid OrderId,
    int Quantity) : DomainEvent;
