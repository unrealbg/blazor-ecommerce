using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.GetShipment;

public sealed record GetShipmentQuery(Guid ShipmentId) : IQuery<ShipmentDto?>;
