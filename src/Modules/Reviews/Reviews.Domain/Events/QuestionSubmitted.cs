using BuildingBlocks.Domain.Primitives;

namespace Reviews.Domain.Events;

public sealed record QuestionSubmitted(Guid QuestionId, Guid ProductId, Guid CustomerId) : DomainEvent;
