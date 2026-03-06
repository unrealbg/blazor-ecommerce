using BuildingBlocks.Domain.Primitives;

namespace Inventory.Domain.Stock.Events;

public sealed record StockReserved(
    Guid ProductId,
    string? Sku,
    Guid ReservationId,
    int Quantity) : DomainEvent;
