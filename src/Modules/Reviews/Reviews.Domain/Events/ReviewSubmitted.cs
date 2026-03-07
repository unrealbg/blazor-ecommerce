using BuildingBlocks.Domain.Primitives;

namespace Reviews.Domain.Events;

public sealed record ReviewSubmitted(Guid ReviewId, Guid ProductId, Guid CustomerId) : DomainEvent;
