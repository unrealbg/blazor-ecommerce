using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.GetShipmentByOrder;

public sealed class GetShipmentByOrderQueryHandler(
    IShipmentRepository shipmentRepository,
    IShipmentEventRepository shipmentEventRepository)
    : IQueryHandler<GetShipmentByOrderQuery, ShipmentDto?>
{
    public async Task<ShipmentDto?> Handle(GetShipmentByOrderQuery request, CancellationToken cancellationToken)
    {
        var shipment = await shipmentRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);
        if (shipment is null)
        {
            return null;
        }

        var events = await shipmentEventRepository.ListByShipmentIdAsync(shipment.Id, cancellationToken);
        return shipment.ToDto(events);
    }
}
