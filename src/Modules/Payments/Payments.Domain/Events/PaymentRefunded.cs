using BuildingBlocks.Domain.Primitives;

namespace Payments.Domain.Events;

public sealed record PaymentRefunded(
    Guid PaymentIntentId,
    Guid OrderId,
    string Provider,
    decimal Amount,
    string Currency) : DomainEvent;
