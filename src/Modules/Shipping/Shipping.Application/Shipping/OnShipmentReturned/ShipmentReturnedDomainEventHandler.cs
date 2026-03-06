using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;
using Microsoft.Extensions.Logging;
using Shipping.Domain.Events;

namespace Shipping.Application.Shipping.OnShipmentReturned;

public sealed class ShipmentReturnedDomainEventHandler(
    IOrderFulfillmentService orderFulfillmentService,
    ILogger<ShipmentReturnedDomainEventHandler> logger)
    : IDomainEventHandler<ShipmentReturned>
{
    public async Task Handle(ShipmentReturned domainEvent, CancellationToken cancellationToken)
    {
        var result = await orderFulfillmentService.MarkReturnedAsync(
            domainEvent.OrderId,
            domainEvent.ShipmentId,
            domainEvent.OccurredOnUtc,
            cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to mark order {OrderId} as returned for shipment {ShipmentId}. Code: {Code}, Message: {Message}",
                domainEvent.OrderId,
                domainEvent.ShipmentId,
                result.Error.Code,
                result.Error.Message);
        }
    }
}
