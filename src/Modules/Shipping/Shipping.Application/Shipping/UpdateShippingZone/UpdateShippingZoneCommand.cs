using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.UpdateShippingZone;

public sealed record UpdateShippingZoneCommand(
    Guid ShippingZoneId,
    string Name,
    IReadOnlyCollection<string> CountryCodes,
    bool IsActive) : ICommand<bool>;
