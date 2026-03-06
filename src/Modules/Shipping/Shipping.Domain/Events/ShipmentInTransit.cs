using BuildingBlocks.Domain.Primitives;

namespace Shipping.Domain.Events;

public sealed record ShipmentInTransit(
    Guid ShipmentId,
    Guid OrderId,
    string CarrierName) : DomainEvent;
