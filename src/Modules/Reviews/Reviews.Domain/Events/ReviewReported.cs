using BuildingBlocks.Domain.Primitives;

namespace Reviews.Domain.Events;

public sealed record ReviewReported(Guid ReviewId, Guid ProductId) : DomainEvent;
