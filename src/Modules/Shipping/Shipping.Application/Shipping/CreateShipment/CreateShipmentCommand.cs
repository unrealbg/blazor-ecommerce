using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.CreateShipment;

public sealed record CreateShipmentCommand(
    Guid OrderId,
    string? ShippingMethodCode) : ICommand<Guid>;
