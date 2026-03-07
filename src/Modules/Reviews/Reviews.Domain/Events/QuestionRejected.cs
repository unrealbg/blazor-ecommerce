using BuildingBlocks.Domain.Primitives;

namespace Reviews.Domain.Events;

public sealed record QuestionRejected(Guid QuestionId, Guid ProductId) : DomainEvent;
