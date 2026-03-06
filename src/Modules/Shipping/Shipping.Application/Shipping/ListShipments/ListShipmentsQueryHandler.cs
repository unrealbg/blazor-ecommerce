using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.ListShipments;

public sealed class ListShipmentsQueryHandler(
    IShipmentRepository shipmentRepository,
    IShipmentEventRepository shipmentEventRepository)
    : IQueryHandler<ListShipmentsQuery, ShippingPage<ShipmentDto>>
{
    public async Task<ShippingPage<ShipmentDto>> Handle(
        ListShipmentsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : Math.Min(100, request.PageSize);

        var shipments = await shipmentRepository.ListAsync(
            request.Status,
            request.OrderId,
            page,
            pageSize,
            cancellationToken);
        var totalCount = await shipmentRepository.CountAsync(request.Status, request.OrderId, cancellationToken);

        var resultItems = new List<ShipmentDto>(shipments.Count);
        foreach (var shipment in shipments)
        {
            var events = await shipmentEventRepository.ListByShipmentIdAsync(shipment.Id, cancellationToken);
            resultItems.Add(shipment.ToDto(events));
        }

        return new ShippingPage<ShipmentDto>(page, pageSize, totalCount, resultItems);
    }
}
