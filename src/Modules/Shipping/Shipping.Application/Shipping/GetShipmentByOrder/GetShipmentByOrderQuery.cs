using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.GetShipmentByOrder;

public sealed record GetShipmentByOrderQuery(Guid OrderId) : IQuery<ShipmentDto?>;
