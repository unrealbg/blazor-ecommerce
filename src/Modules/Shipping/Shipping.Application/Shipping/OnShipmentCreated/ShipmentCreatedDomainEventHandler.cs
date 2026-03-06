using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;
using Microsoft.Extensions.Logging;
using Shipping.Domain.Events;

namespace Shipping.Application.Shipping.OnShipmentCreated;

public sealed class ShipmentCreatedDomainEventHandler(
    IOrderFulfillmentService orderFulfillmentService,
    ILogger<ShipmentCreatedDomainEventHandler> logger)
    : IDomainEventHandler<ShipmentCreated>
{
    public async Task Handle(ShipmentCreated domainEvent, CancellationToken cancellationToken)
    {
        var result = await orderFulfillmentService.MarkFulfillmentPendingAsync(
            domainEvent.OrderId,
            domainEvent.ShipmentId,
            domainEvent.OccurredOnUtc,
            cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed to mark order {OrderId} as fulfillment pending for shipment {ShipmentId}. Code: {Code}, Message: {Message}",
                domainEvent.OrderId,
                domainEvent.ShipmentId,
                result.Error.Code,
                result.Error.Message);
        }
    }
}
