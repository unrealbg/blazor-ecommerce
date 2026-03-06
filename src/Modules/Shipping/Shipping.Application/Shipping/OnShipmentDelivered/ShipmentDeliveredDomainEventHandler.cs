using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;
using Microsoft.Extensions.Logging;
using Shipping.Domain.Events;

namespace Shipping.Application.Shipping.OnShipmentDelivered;

public sealed class ShipmentDeliveredDomainEventHandler(
    IOrderFulfillmentService orderFulfillmentService,
    ILogger<ShipmentDeliveredDomainEventHandler> logger)
    : IDomainEventHandler<ShipmentDelivered>
{
    public async Task Handle(ShipmentDelivered domainEvent, CancellationToken cancellationToken)
    {
        var result = await orderFulfillmentService.MarkFulfilledAsync(
            domainEvent.OrderId,
            domainEvent.ShipmentId,
            domainEvent.OccurredOnUtc,
            cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to mark order {OrderId} as fulfilled for shipment {ShipmentId}. Code: {Code}, Message: {Message}",
                domainEvent.OrderId,
                domainEvent.ShipmentId,
                result.Error.Code,
                result.Error.Message);
        }
    }
}
