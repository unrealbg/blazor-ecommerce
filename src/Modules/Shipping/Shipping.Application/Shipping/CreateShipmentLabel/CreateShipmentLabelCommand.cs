using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.CreateShipmentLabel;

public sealed record CreateShipmentLabelCommand(Guid ShipmentId) : ICommand<bool>;
