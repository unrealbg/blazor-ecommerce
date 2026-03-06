using BuildingBlocks.Domain.Primitives;

namespace Shipping.Domain.Events;

public sealed record ShipmentShipped(
    Guid ShipmentId,
    Guid OrderId,
    string CarrierName) : DomainEvent;
