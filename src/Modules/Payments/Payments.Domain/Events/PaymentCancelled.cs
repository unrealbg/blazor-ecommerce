using BuildingBlocks.Domain.Primitives;

namespace Payments.Domain.Events;

public sealed record PaymentCancelled(
    Guid PaymentIntentId,
    Guid OrderId,
    string Provider,
    string? Reason) : DomainEvent;
