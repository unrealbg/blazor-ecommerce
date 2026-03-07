using BuildingBlocks.Domain.Primitives;

namespace Reviews.Domain.Events;

public sealed record AnswerApproved(Guid QuestionId, Guid AnswerId, Guid ProductId) : DomainEvent;
