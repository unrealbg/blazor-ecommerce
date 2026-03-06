using BuildingBlocks.Domain.Primitives;

namespace Shipping.Domain.Events;

public sealed record ShipmentReturned(
    Guid ShipmentId,
    Guid OrderId,
    string CarrierName,
    string? Reason) : DomainEvent;
