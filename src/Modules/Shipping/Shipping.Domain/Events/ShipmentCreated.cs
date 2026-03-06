using BuildingBlocks.Domain.Primitives;

namespace Shipping.Domain.Events;

public sealed record ShipmentCreated(
    Guid ShipmentId,
    Guid OrderId,
    string CarrierName) : DomainEvent;
