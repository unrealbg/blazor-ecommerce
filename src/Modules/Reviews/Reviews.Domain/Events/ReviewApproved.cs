using BuildingBlocks.Domain.Primitives;

namespace Reviews.Domain.Events;

public sealed record ReviewApproved(Guid ReviewId, Guid ProductId) : DomainEvent;
