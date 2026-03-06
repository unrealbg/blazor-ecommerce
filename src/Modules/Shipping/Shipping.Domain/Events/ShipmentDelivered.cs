using BuildingBlocks.Domain.Primitives;

namespace Shipping.Domain.Events;

public sealed record ShipmentDelivered(
    Guid ShipmentId,
    Guid OrderId,
    string CarrierName) : DomainEvent;
