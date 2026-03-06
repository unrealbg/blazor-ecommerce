using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.CancelShipment;

public sealed record CancelShipmentCommand(
    Guid ShipmentId,
    string? Reason) : ICommand<bool>;
