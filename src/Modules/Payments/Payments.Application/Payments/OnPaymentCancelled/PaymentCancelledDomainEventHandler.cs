using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;
using Microsoft.Extensions.Logging;
using Payments.Domain.Events;

namespace Payments.Application.Payments.OnPaymentCancelled;

public sealed class PaymentCancelledDomainEventHandler(
    IOrderPaymentService orderPaymentService,
    IInventoryReservationService inventoryReservationService,
    ILogger<PaymentCancelledDomainEventHandler> logger)
    : IDomainEventHandler<PaymentCancelled>
{
    public async Task Handle(PaymentCancelled domainEvent, CancellationToken cancellationToken)
    {
        var markResult = await orderPaymentService.MarkCancelledAsync(
            domainEvent.OrderId,
            domainEvent.PaymentIntentId,
            domainEvent.Reason,
            domainEvent.OccurredOnUtc,
            cancellationToken);

        if (markResult.IsFailure)
        {
            logger.LogWarning(
                "Failed marking order {OrderId} as cancelled. Code: {Code}, Message: {Message}",
                domainEvent.OrderId,
                markResult.Error.Code,
                markResult.Error.Message);
        }

        var releaseResult = await inventoryReservationService.ReleaseOrderReservationsAsync(
            domainEvent.OrderId,
            cancellationToken);

        if (releaseResult.IsFailure)
        {
            logger.LogError(
                "Failed releasing inventory reservations for cancelled order {OrderId}. Code: {Code}, Message: {Message}",
                domainEvent.OrderId,
                releaseResult.Error.Code,
                releaseResult.Error.Message);
        }
    }
}
