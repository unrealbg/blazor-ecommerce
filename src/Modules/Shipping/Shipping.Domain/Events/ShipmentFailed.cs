using BuildingBlocks.Domain.Primitives;

namespace Shipping.Domain.Events;

public sealed record ShipmentFailed(
    Guid ShipmentId,
    Guid OrderId,
    string CarrierName,
    string? Reason) : DomainEvent;
