using BuildingBlocks.Domain.Primitives;

namespace Reviews.Domain.Events;

public sealed record ReviewHidden(Guid ReviewId, Guid ProductId) : DomainEvent;
