using Backoffice.Application.Backoffice;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Payments.Application.Webhooks.ProcessWebhook;
using Payments.Domain.Payments;
using Payments.Infrastructure.Persistence;
using Shipping.Application.Webhooks.ProcessCarrierWebhook;
using Shipping.Domain.Shipping;
using Shipping.Infrastructure.Persistence;

namespace Backoffice.Infrastructure.Services;

internal sealed class SystemOperationsService(
    OutboxDbContext outboxDbContext,
    PaymentsDbContext paymentsDbContext,
    ShippingDbContext shippingDbContext,
    ISender sender)
    : ISystemOperationsService
{
    public async Task<Result> RetryOutboxMessageAsync(Guid outboxMessageId, CancellationToken cancellationToken)
    {
        var message = await outboxDbContext.OutboxMessages.SingleOrDefaultAsync(item => item.Id == outboxMessageId, cancellationToken);
        if (message is null)
        {
            return Result.Failure(new Error("backoffice.outbox.not_found", "Outbox message was not found."));
        }

        message.ResetForRetry();
        await outboxDbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<bool>> ReprocessPaymentWebhookAsync(Guid webhookMessageId, CancellationToken cancellationToken)
    {
        var message = await paymentsDbContext.WebhookInboxMessages
            .SingleOrDefaultAsync(item => item.Id == webhookMessageId, cancellationToken);
        if (message is null)
        {
            return Result<bool>.Failure(new Error("backoffice.payment_webhook.not_found", "Payment webhook inbox message was not found."));
        }

        if (string.Equals(message.Payload, "{}", StringComparison.Ordinal))
        {
            return Result<bool>.Failure(new Error("backoffice.payment_webhook.payload_unavailable", "Payment webhook payload has already been redacted by retention."));
        }

        if (message.ProcessingStatus == WebhookInboxProcessingStatus.Failed)
        {
            message.RequeueForProcessing();
            await paymentsDbContext.SaveChangesAsync(cancellationToken);
        }

        return await sender.Send(new ProcessWebhookCommand(message.Provider, message.Payload), cancellationToken);
    }

    public async Task<Result<bool>> ReprocessShippingWebhookAsync(Guid webhookMessageId, CancellationToken cancellationToken)
    {
        var message = await shippingDbContext.CarrierWebhookInboxMessages
            .SingleOrDefaultAsync(item => item.Id == webhookMessageId, cancellationToken);
        if (message is null)
        {
            return Result<bool>.Failure(new Error("backoffice.shipping_webhook.not_found", "Shipping webhook inbox message was not found."));
        }

        if (string.Equals(message.Payload, "{}", StringComparison.Ordinal))
        {
            return Result<bool>.Failure(new Error("backoffice.shipping_webhook.payload_unavailable", "Shipping webhook payload has already been redacted by retention."));
        }

        if (message.ProcessingStatus == CarrierWebhookInboxProcessingStatus.Failed)
        {
            message.RequeueForProcessing();
            await shippingDbContext.SaveChangesAsync(cancellationToken);
        }

        return await sender.Send(new ProcessCarrierWebhookCommand(message.Provider, message.Payload), cancellationToken);
    }
}