using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Payments.Application.Providers;
using Payments.Domain.Payments;

namespace Payments.Application.Payments.ConfirmPaymentIntent;

public sealed class ConfirmPaymentIntentCommandHandler(
    IPaymentIntentRepository paymentIntentRepository,
    IPaymentTransactionRepository paymentTransactionRepository,
    IPaymentIdempotencyRepository paymentIdempotencyRepository,
    IPaymentProviderFactory paymentProviderFactory,
    IPaymentsUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<ConfirmPaymentIntentCommand, PaymentIntentActionResult>
{
    private const string ConfirmOperation = "confirm-intent";

    public async Task<Result<PaymentIntentActionResult>> Handle(
        ConfirmPaymentIntentCommand request,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = request.IdempotencyKey.Trim();

        var existingRecord = await paymentIdempotencyRepository.GetByOperationAndKeyAsync(
            ConfirmOperation,
            idempotencyKey,
            cancellationToken);
        if (existingRecord is not null)
        {
            var existingIntent = await paymentIntentRepository.GetByIdAsync(existingRecord.PaymentIntentId, cancellationToken);
            if (existingIntent is not null)
            {
                return Result<PaymentIntentActionResult>.Success(PaymentIntentMappings.ToActionResult(
                    existingIntent,
                    existingIntent.Status == PaymentIntentStatus.RequiresAction,
                    null));
            }
        }

        return await unitOfWork.ExecuteInTransactionAsync(
            async innerCancellationToken =>
            {
                var idempotencyInTransaction = await paymentIdempotencyRepository.GetByOperationAndKeyAsync(
                    ConfirmOperation,
                    idempotencyKey,
                    innerCancellationToken);
                if (idempotencyInTransaction is not null)
                {
                    var idempotentIntent = await paymentIntentRepository.GetByIdAsync(
                        idempotencyInTransaction.PaymentIntentId,
                        innerCancellationToken);
                    if (idempotentIntent is not null)
                    {
                        return Result<PaymentIntentActionResult>.Success(PaymentIntentMappings.ToActionResult(
                            idempotentIntent,
                            idempotentIntent.Status == PaymentIntentStatus.RequiresAction,
                            null));
                    }
                }

                var paymentIntentEntity = await paymentIntentRepository.GetByIdAsync(
                    request.PaymentIntentId,
                    innerCancellationToken);
                if (paymentIntentEntity is null)
                {
                    return Result<PaymentIntentActionResult>.Failure(new Error(
                        "payments.intent.not_found",
                        "Payment intent was not found."));
                }

                if (paymentIntentEntity.Status is PaymentIntentStatus.Captured or
                    PaymentIntentStatus.Refunded or
                    PaymentIntentStatus.PartiallyRefunded)
                {
                    var idempotencyRecordResult = PaymentIdempotencyRecord.Create(
                        ConfirmOperation,
                        idempotencyKey,
                        paymentIntentEntity.Id,
                        clock.UtcNow);
                    if (idempotencyRecordResult.IsSuccess)
                    {
                        await paymentIdempotencyRepository.AddAsync(idempotencyRecordResult.Value, innerCancellationToken);
                        await unitOfWork.SaveChangesAsync(innerCancellationToken);
                    }

                    return Result<PaymentIntentActionResult>.Success(PaymentIntentMappings.ToActionResult(
                        paymentIntentEntity,
                        requiresAction: false,
                        redirectUrl: null));
                }

                if (paymentIntentEntity.Status is PaymentIntentStatus.Cancelled or PaymentIntentStatus.Failed)
                {
                    return Result<PaymentIntentActionResult>.Failure(new Error(
                        "payments.intent.already_completed",
                        "Payment intent is in a terminal state and cannot be confirmed."));
                }

                if (string.IsNullOrWhiteSpace(paymentIntentEntity.ProviderPaymentIntentId))
                {
                    return Result<PaymentIntentActionResult>.Failure(new Error(
                        "payments.intent.provider_reference.required",
                        "Provider payment intent id is missing."));
                }

                IPaymentProvider provider;
                try
                {
                    provider = paymentProviderFactory.Resolve(paymentIntentEntity.Provider);
                }
                catch (InvalidOperationException)
                {
                    return Result<PaymentIntentActionResult>.Failure(new Error(
                        "payments.provider.unavailable",
                        "Payment provider is unavailable."));
                }

                PaymentProviderConfirmResponse providerResponse;
                try
                {
                    providerResponse = await provider.ConfirmPaymentAsync(
                        new PaymentProviderConfirmRequest(
                            paymentIntentEntity.Id,
                            paymentIntentEntity.ProviderPaymentIntentId,
                            paymentIntentEntity.Amount,
                            paymentIntentEntity.Currency,
                            new Dictionary<string, string>
                            {
                                ["paymentIntentId"] = paymentIntentEntity.Id.ToString("D"),
                                ["orderId"] = paymentIntentEntity.OrderId.ToString("D"),
                            }),
                        innerCancellationToken);
                }
                catch (Exception)
                {
                    return Result<PaymentIntentActionResult>.Failure(new Error(
                        "payments.provider.unavailable",
                        "Payment provider is unavailable."));
                }

                var applyResult = paymentIntentEntity.ApplyProviderConfirmation(
                    providerResponse.Status,
                    providerResponse.FailureCode,
                    providerResponse.FailureMessage,
                    clock.UtcNow);
                if (applyResult.IsFailure)
                {
                    return Result<PaymentIntentActionResult>.Failure(applyResult.Error);
                }

                var transactionType = ResolveTransactionType(providerResponse.Status);
                var createTransactionResult = PaymentTransaction.Create(
                    paymentIntentEntity.Id,
                    transactionType,
                    providerResponse.ProviderTransactionId,
                    paymentIntentEntity.Amount,
                    paymentIntentEntity.Currency,
                    providerResponse.Status.ToString(),
                    providerResponse.RawReference,
                    providerResponse.MetadataJson,
                    clock.UtcNow);
                if (createTransactionResult.IsFailure)
                {
                    return Result<PaymentIntentActionResult>.Failure(createTransactionResult.Error);
                }

                await paymentTransactionRepository.AddAsync(createTransactionResult.Value, innerCancellationToken);

                var createIdempotencyResult = PaymentIdempotencyRecord.Create(
                    ConfirmOperation,
                    idempotencyKey,
                    paymentIntentEntity.Id,
                    clock.UtcNow);
                if (createIdempotencyResult.IsFailure)
                {
                    return Result<PaymentIntentActionResult>.Failure(createIdempotencyResult.Error);
                }

                await paymentIdempotencyRepository.AddAsync(createIdempotencyResult.Value, innerCancellationToken);
                await unitOfWork.SaveChangesAsync(innerCancellationToken);

                return Result<PaymentIntentActionResult>.Success(PaymentIntentMappings.ToActionResult(
                    paymentIntentEntity,
                    providerResponse.RequiresAction,
                    providerResponse.RedirectUrl));
            },
            cancellationToken);
    }

    private static PaymentTransactionType ResolveTransactionType(PaymentIntentStatus status)
    {
        return status switch
        {
            PaymentIntentStatus.Captured => PaymentTransactionType.Capture,
            PaymentIntentStatus.Cancelled => PaymentTransactionType.Cancellation,
            PaymentIntentStatus.Failed => PaymentTransactionType.Failure,
            PaymentIntentStatus.Refunded or PaymentIntentStatus.PartiallyRefunded => PaymentTransactionType.Refund,
            _ => PaymentTransactionType.Authorization,
        };
    }
}
