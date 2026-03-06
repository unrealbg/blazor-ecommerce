using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.ListShippingZones;

public sealed class ListShippingZonesQueryHandler(IShippingZoneRepository repository)
    : IQueryHandler<ListShippingZonesQuery, IReadOnlyCollection<ShippingZoneDto>>
{
    public async Task<IReadOnlyCollection<ShippingZoneDto>> Handle(
        ListShippingZonesQuery request,
        CancellationToken cancellationToken)
    {
        var zones = await repository.ListAsync(request.ActiveOnly, cancellationToken);
        return zones.Select(ShippingMappings.ToDto).ToList();
    }
}
