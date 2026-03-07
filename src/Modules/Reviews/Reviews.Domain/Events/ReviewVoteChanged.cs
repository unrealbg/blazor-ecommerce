using BuildingBlocks.Domain.Primitives;

namespace Reviews.Domain.Events;

public sealed record ReviewVoteChanged(Guid ReviewId, Guid ProductId) : DomainEvent;
