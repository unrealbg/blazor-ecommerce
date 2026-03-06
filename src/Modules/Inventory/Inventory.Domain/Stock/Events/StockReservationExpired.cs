using BuildingBlocks.Domain.Primitives;

namespace Inventory.Domain.Stock.Events;

public sealed record StockReservationExpired(
    Guid ProductId,
    string? Sku,
    Guid ReservationId,
    int Quantity) : DomainEvent;
