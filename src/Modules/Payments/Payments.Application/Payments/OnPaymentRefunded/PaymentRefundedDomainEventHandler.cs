using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;
using Microsoft.Extensions.Logging;
using Payments.Domain.Events;

namespace Payments.Application.Payments.OnPaymentRefunded;

public sealed class PaymentRefundedDomainEventHandler(
    IOrderPaymentService orderPaymentService,
    ILogger<PaymentRefundedDomainEventHandler> logger)
    : IDomainEventHandler<PaymentRefunded>
{
    public async Task Handle(PaymentRefunded domainEvent, CancellationToken cancellationToken)
    {
        var result = await orderPaymentService.MarkRefundedAsync(
            domainEvent.OrderId,
            domainEvent.PaymentIntentId,
            partial: false,
            domainEvent.OccurredOnUtc,
            cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed marking order {OrderId} as refunded. Code: {Code}, Message: {Message}",
                domainEvent.OrderId,
                result.Error.Code,
                result.Error.Message);
        }
    }
}
