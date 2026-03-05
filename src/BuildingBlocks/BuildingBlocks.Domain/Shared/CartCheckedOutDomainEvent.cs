using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Shared;

public sealed record CartCheckedOutDomainEvent(
    Guid CartId,
    Guid CustomerId,
    string Currency,
    decimal TotalAmount) : DomainEvent;
