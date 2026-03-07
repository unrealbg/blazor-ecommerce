using BuildingBlocks.Domain.Primitives;

namespace Reviews.Domain.Events;

public sealed record ReviewAggregateUpdated(Guid ProductId, int ApprovedReviewCount, decimal AverageRating) : DomainEvent;
