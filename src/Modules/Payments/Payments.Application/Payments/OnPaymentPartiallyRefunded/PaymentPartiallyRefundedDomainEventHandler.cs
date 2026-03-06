using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;
using Microsoft.Extensions.Logging;
using Payments.Domain.Events;

namespace Payments.Application.Payments.OnPaymentPartiallyRefunded;

public sealed class PaymentPartiallyRefundedDomainEventHandler(
    IOrderPaymentService orderPaymentService,
    ILogger<PaymentPartiallyRefundedDomainEventHandler> logger)
    : IDomainEventHandler<PaymentPartiallyRefunded>
{
    public async Task Handle(PaymentPartiallyRefunded domainEvent, CancellationToken cancellationToken)
    {
        var result = await orderPaymentService.MarkRefundedAsync(
            domainEvent.OrderId,
            domainEvent.PaymentIntentId,
            partial: true,
            domainEvent.OccurredOnUtc,
            cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Failed marking order {OrderId} as partially refunded. Code: {Code}, Message: {Message}",
                domainEvent.OrderId,
                result.Error.Code,
                result.Error.Message);
        }
    }
}
