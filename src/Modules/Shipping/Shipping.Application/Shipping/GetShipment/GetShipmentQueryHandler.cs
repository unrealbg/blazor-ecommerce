using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.GetShipment;

public sealed class GetShipmentQueryHandler(
    IShipmentRepository shipmentRepository,
    IShipmentEventRepository shipmentEventRepository)
    : IQueryHandler<GetShipmentQuery, ShipmentDto?>
{
    public async Task<ShipmentDto?> Handle(GetShipmentQuery request, CancellationToken cancellationToken)
    {
        var shipment = await shipmentRepository.GetByIdAsync(request.ShipmentId, cancellationToken);
        if (shipment is null)
        {
            return null;
        }

        var events = await shipmentEventRepository.ListByShipmentIdAsync(shipment.Id, cancellationToken);
        return shipment.ToDto(events);
    }
}
