using BuildingBlocks.Domain.Primitives;

namespace Reviews.Domain.Events;

public sealed record ReviewRejected(Guid ReviewId, Guid ProductId) : DomainEvent;
