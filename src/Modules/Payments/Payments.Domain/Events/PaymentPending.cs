using BuildingBlocks.Domain.Primitives;

namespace Payments.Domain.Events;

public sealed record PaymentPending(
    Guid PaymentIntentId,
    Guid OrderId,
    string Provider) : DomainEvent;
