using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.ListShippingZones;

public sealed record ListShippingZonesQuery(bool ActiveOnly) : IQuery<IReadOnlyCollection<ShippingZoneDto>>;
