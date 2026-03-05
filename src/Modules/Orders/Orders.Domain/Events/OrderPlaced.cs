using BuildingBlocks.Domain.Primitives;

namespace Orders.Domain.Events;

public sealed record OrderPlaced(
    Guid OrderId,
    string CustomerId,
    string Currency,
    decimal TotalAmount) : DomainEvent;
