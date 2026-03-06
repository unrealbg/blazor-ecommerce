using BuildingBlocks.Domain.Primitives;

namespace Shipping.Domain.Events;

public sealed record ShipmentLabelCreated(
    Guid ShipmentId,
    Guid OrderId,
    string CarrierName) : DomainEvent;
