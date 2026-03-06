using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.CreateShippingZone;

public sealed record CreateShippingZoneCommand(
    string Code,
    string Name,
    IReadOnlyCollection<string> CountryCodes) : ICommand<Guid>;
