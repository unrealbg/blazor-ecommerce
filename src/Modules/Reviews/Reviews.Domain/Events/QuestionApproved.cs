using BuildingBlocks.Domain.Primitives;

namespace Reviews.Domain.Events;

public sealed record QuestionApproved(Guid QuestionId, Guid ProductId) : DomainEvent;
