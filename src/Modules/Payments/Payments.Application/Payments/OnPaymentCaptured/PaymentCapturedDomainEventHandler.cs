using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;
using Microsoft.Extensions.Logging;
using Payments.Domain.Events;

namespace Payments.Application.Payments.OnPaymentCaptured;

public sealed class PaymentCapturedDomainEventHandler(
    IOrderPaymentService orderPaymentService,
    IInventoryReservationService inventoryReservationService,
    ILogger<PaymentCapturedDomainEventHandler> logger)
    : IDomainEventHandler<PaymentCaptured>
{
    public async Task Handle(PaymentCaptured domainEvent, CancellationToken cancellationToken)
    {
        var orderSnapshot = await orderPaymentService.GetByIdAsync(domainEvent.OrderId, cancellationToken);
        if (orderSnapshot is null)
        {
            logger.LogWarning(
                "Skipping payment captured synchronization because order {OrderId} was not found.",
                domainEvent.OrderId);
            return;
        }

        var markOrderResult = await orderPaymentService.MarkPaidAsync(
            domainEvent.OrderId,
            domainEvent.PaymentIntentId,
            domainEvent.OccurredOnUtc,
            cancellationToken);

        if (markOrderResult.IsFailure)
        {
            logger.LogWarning(
                "Failed to mark order {OrderId} as paid. Code: {Code}, Message: {Message}",
                domainEvent.OrderId,
                markOrderResult.Error.Code,
                markOrderResult.Error.Message);
            return;
        }

        var consumeResult = await inventoryReservationService.ConsumeOrderReservationsAsync(
            domainEvent.OrderId,
            orderSnapshot.Lines
                .Select(line => new InventoryCartLineRequest(line.ProductId, line.Sku, line.Quantity))
                .ToList(),
            cancellationToken);

        if (consumeResult.IsFailure)
        {
            logger.LogError(
                "Failed consuming inventory reservations for paid order {OrderId}. Code: {Code}, Message: {Message}",
                domainEvent.OrderId,
                consumeResult.Error.Code,
                consumeResult.Error.Message);
        }
    }
}
