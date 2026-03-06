using BuildingBlocks.Domain.Primitives;

namespace Payments.Domain.Events;

public sealed record PaymentAuthorized(
    Guid PaymentIntentId,
    Guid OrderId,
    string Provider) : DomainEvent;
