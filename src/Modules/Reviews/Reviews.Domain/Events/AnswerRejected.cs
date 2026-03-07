using BuildingBlocks.Domain.Primitives;

namespace Reviews.Domain.Events;

public sealed record AnswerRejected(Guid QuestionId, Guid AnswerId, Guid ProductId) : DomainEvent;
