using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Microsoft.Extensions.Logging;
using Payments.Application.Payments;
using Payments.Application.Providers;
using Payments.Domain.Payments;

namespace Payments.Application.Webhooks.ProcessWebhook;

public sealed class ProcessWebhookCommandHandler(
    IWebhookInboxRepository webhookInboxRepository,
    IPaymentIntentRepository paymentIntentRepository,
    IPaymentTransactionRepository paymentTransactionRepository,
    IPaymentsUnitOfWork unitOfWork,
    IPaymentProviderFactory paymentProviderFactory,
    IClock clock,
    ILogger<ProcessWebhookCommandHandler> logger)
    : ICommandHandler<ProcessWebhookCommand, bool>
{
    public async Task<Result<bool>> Handle(ProcessWebhookCommand request, CancellationToken cancellationToken)
    {
        IPaymentProvider provider;
        try
        {
            provider = paymentProviderFactory.Resolve(request.Provider);
        }
        catch (InvalidOperationException)
        {
            return Result<bool>.Failure(new Error(
                "payments.provider.unavailable",
                "Webhook provider is unavailable."));
        }

        PaymentProviderWebhookResult webhookResult;
        try
        {
            webhookResult = await provider.ParseWebhookAsync(request.Payload, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to parse webhook payload for provider {Provider}", provider.Name);
            return Result<bool>.Failure(new Error(
                "payments.webhook.parse_failed",
                "Webhook payload could not be parsed."));
        }

        var existingMessage = await webhookInboxRepository.GetByProviderAndExternalEventIdAsync(
            provider.Name,
            webhookResult.ExternalEventId,
            cancellationToken);
        if (existingMessage is not null)
        {
            return Result<bool>.Success(false);
        }

        return await unitOfWork.ExecuteInTransactionAsync(
            async innerCancellationToken =>
            {
                var inboxResult = WebhookInboxMessage.Create(
                    provider.Name,
                    webhookResult.ExternalEventId,
                    webhookResult.EventType,
                    request.Payload,
                    clock.UtcNow);
                if (inboxResult.IsFailure)
                {
                    return Result<bool>.Failure(inboxResult.Error);
                }

                var inboxMessage = inboxResult.Value;
                await webhookInboxRepository.AddAsync(inboxMessage, innerCancellationToken);

                try
                {
                    var paymentIntentEntity = await paymentIntentRepository.GetByProviderIntentIdAsync(
                        provider.Name,
                        webhookResult.ProviderPaymentIntentId,
                        innerCancellationToken);
                    if (paymentIntentEntity is null)
                    {
                        inboxMessage.MarkIgnored(clock.UtcNow, "Payment intent was not found.");
                        await unitOfWork.SaveChangesAsync(innerCancellationToken);
                        return Result<bool>.Success(false);
                    }

                    var statusUpdateResult = ApplyStatusFromWebhook(paymentIntentEntity, webhookResult, clock.UtcNow);
                    if (statusUpdateResult.IsFailure)
                    {
                        inboxMessage.MarkFailed(clock.UtcNow, statusUpdateResult.Error.Message);
                        await unitOfWork.SaveChangesAsync(innerCancellationToken);
                        return Result<bool>.Failure(statusUpdateResult.Error);
                    }

                    var transactionResult = PaymentTransaction.Create(
                        paymentIntentEntity.Id,
                        ResolveTransactionType(webhookResult.Status),
                        webhookResult.ProviderTransactionId,
                        webhookResult.Amount ?? paymentIntentEntity.Amount,
                        webhookResult.Currency ?? paymentIntentEntity.Currency,
                        webhookResult.Status.ToString(),
                        webhookResult.RawReference,
                        webhookResult.MetadataJson,
                        clock.UtcNow);
                    if (transactionResult.IsFailure)
                    {
                        inboxMessage.MarkFailed(clock.UtcNow, transactionResult.Error.Message);
                        await unitOfWork.SaveChangesAsync(innerCancellationToken);
                        return Result<bool>.Failure(transactionResult.Error);
                    }

                    await paymentTransactionRepository.AddAsync(transactionResult.Value, innerCancellationToken);
                    inboxMessage.MarkProcessed(clock.UtcNow);
                    await unitOfWork.SaveChangesAsync(innerCancellationToken);
                    return Result<bool>.Success(true);
                }
                catch (Exception exception)
                {
                    inboxMessage.MarkFailed(clock.UtcNow, exception.ToString());
                    await unitOfWork.SaveChangesAsync(innerCancellationToken);
                    logger.LogError(exception, "Webhook processing failed for provider {Provider}", provider.Name);
                    return Result<bool>.Failure(new Error(
                        "payments.webhook.processing_failed",
                        "Webhook processing failed."));
                }
            },
            cancellationToken);
    }

    private static Result ApplyStatusFromWebhook(
        PaymentIntent paymentIntent,
        PaymentProviderWebhookResult webhookResult,
        DateTime utcNow)
    {
        return webhookResult.Status switch
        {
            PaymentIntentStatus.Cancelled => paymentIntent.ApplyProviderCancellation(
                webhookResult.FailureMessage,
                utcNow),
            PaymentIntentStatus.Refunded => paymentIntent.ApplyProviderRefund(
                partial: false,
                utcNow),
            PaymentIntentStatus.PartiallyRefunded => paymentIntent.ApplyProviderRefund(
                partial: true,
                utcNow),
            _ => paymentIntent.ApplyProviderConfirmation(
                webhookResult.Status,
                webhookResult.FailureCode,
                webhookResult.FailureMessage,
                utcNow),
        };
    }

    private static PaymentTransactionType ResolveTransactionType(PaymentIntentStatus status)
    {
        return status switch
        {
            PaymentIntentStatus.Captured => PaymentTransactionType.Capture,
            PaymentIntentStatus.Failed => PaymentTransactionType.Failure,
            PaymentIntentStatus.Cancelled => PaymentTransactionType.Cancellation,
            PaymentIntentStatus.Refunded or PaymentIntentStatus.PartiallyRefunded => PaymentTransactionType.Refund,
            _ => PaymentTransactionType.WebhookEvent,
        };
    }
}
