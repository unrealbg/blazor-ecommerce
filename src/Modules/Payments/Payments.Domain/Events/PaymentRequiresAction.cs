using BuildingBlocks.Domain.Primitives;

namespace Payments.Domain.Events;

public sealed record PaymentRequiresAction(
    Guid PaymentIntentId,
    Guid OrderId,
    string Provider) : DomainEvent;
