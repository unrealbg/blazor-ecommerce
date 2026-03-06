using BuildingBlocks.Domain.Primitives;

namespace Payments.Domain.Events;

public sealed record PaymentFailed(
    Guid PaymentIntentId,
    Guid OrderId,
    string Provider,
    string? FailureCode,
    string? FailureMessage) : DomainEvent;
