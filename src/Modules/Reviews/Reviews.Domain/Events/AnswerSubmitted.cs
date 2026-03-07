using BuildingBlocks.Domain.Primitives;

namespace Reviews.Domain.Events;

public sealed record AnswerSubmitted(Guid QuestionId, Guid AnswerId, Guid ProductId) : DomainEvent;
