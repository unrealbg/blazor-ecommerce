using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.MarkShipmentShipped;

public sealed record MarkShipmentShippedCommand(Guid ShipmentId) : ICommand<bool>;
