using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;
using Microsoft.Extensions.Logging;
using Payments.Domain.Events;

namespace Payments.Application.Payments.OnPaymentFailed;

public sealed class PaymentFailedDomainEventHandler(
    IOrderPaymentService orderPaymentService,
    IInventoryReservationService inventoryReservationService,
    ILogger<PaymentFailedDomainEventHandler> logger)
    : IDomainEventHandler<PaymentFailed>
{
    public async Task Handle(PaymentFailed domainEvent, CancellationToken cancellationToken)
    {
        var markResult = await orderPaymentService.MarkPaymentFailedAsync(
            domainEvent.OrderId,
            domainEvent.PaymentIntentId,
            domainEvent.FailureMessage,
            domainEvent.OccurredOnUtc,
            cancellationToken);

        if (markResult.IsFailure)
        {
            logger.LogWarning(
                "Failed marking order {OrderId} as payment failed. Code: {Code}, Message: {Message}",
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
                "Failed releasing inventory reservations for order {OrderId} after payment failure. Code: {Code}, Message: {Message}",
                domainEvent.OrderId,
                releaseResult.Error.Code,
                releaseResult.Error.Message);
        }
    }
}
