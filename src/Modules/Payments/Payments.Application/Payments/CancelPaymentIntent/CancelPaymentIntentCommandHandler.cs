using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Payments.Application.Providers;
using Payments.Domain.Payments;

namespace Payments.Application.Payments.CancelPaymentIntent;

public sealed class CancelPaymentIntentCommandHandler(
    IPaymentIntentRepository paymentIntentRepository,
    IPaymentTransactionRepository paymentTransactionRepository,
    IPaymentProviderFactory paymentProviderFactory,
    IPaymentsUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<CancelPaymentIntentCommand, PaymentIntentActionResult>
{
    public async Task<Result<PaymentIntentActionResult>> Handle(
        CancelPaymentIntentCommand request,
        CancellationToken cancellationToken)
    {
        var paymentIntentEntity = await paymentIntentRepository.GetByIdAsync(request.PaymentIntentId, cancellationToken);
        if (paymentIntentEntity is null)
        {
            return Result<PaymentIntentActionResult>.Failure(new Error(
                "payments.intent.not_found",
                "Payment intent was not found."));
        }

        if (paymentIntentEntity.Status == PaymentIntentStatus.Cancelled)
        {
            return Result<PaymentIntentActionResult>.Success(PaymentIntentMappings.ToActionResult(
                paymentIntentEntity,
                requiresAction: false,
                redirectUrl: null));
        }

        if (paymentIntentEntity.Status is
            PaymentIntentStatus.Captured or
            PaymentIntentStatus.Refunded or
            PaymentIntentStatus.PartiallyRefunded)
        {
            return Result<PaymentIntentActionResult>.Failure(new Error(
                "payments.intent.already_completed",
                "Cannot cancel a completed payment intent."));
        }

        return await unitOfWork.ExecuteInTransactionAsync(
            async innerCancellationToken =>
            {
                PaymentProviderCancelResponse providerResponse;

                if (string.IsNullOrWhiteSpace(paymentIntentEntity.ProviderPaymentIntentId))
                {
                    providerResponse = new PaymentProviderCancelResponse(
                        PaymentIntentStatus.Cancelled,
                        "cancelled",
                        request.Reason,
                        ProviderTransactionId: null,
                        RawReference: null,
                        MetadataJson: null);
                }
                else
                {
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

                    try
                    {
                        providerResponse = await provider.CancelPaymentAsync(
                            new PaymentProviderCancelRequest(
                                paymentIntentEntity.Id,
                                paymentIntentEntity.ProviderPaymentIntentId,
                                request.Reason,
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
                }

                var applyResult = paymentIntentEntity.ApplyProviderCancellation(
                    providerResponse.FailureMessage ?? request.Reason,
                    clock.UtcNow);
                if (applyResult.IsFailure)
                {
                    return Result<PaymentIntentActionResult>.Failure(applyResult.Error);
                }

                var createTransactionResult = PaymentTransaction.Create(
                    paymentIntentEntity.Id,
                    PaymentTransactionType.Cancellation,
                    providerResponse.ProviderTransactionId,
                    paymentIntentEntity.Amount,
                    paymentIntentEntity.Currency,
                    paymentIntentEntity.Status.ToString(),
                    providerResponse.RawReference,
                    providerResponse.MetadataJson,
                    clock.UtcNow);
                if (createTransactionResult.IsFailure)
                {
                    return Result<PaymentIntentActionResult>.Failure(createTransactionResult.Error);
                }

                await paymentTransactionRepository.AddAsync(createTransactionResult.Value, innerCancellationToken);
                await unitOfWork.SaveChangesAsync(innerCancellationToken);

                return Result<PaymentIntentActionResult>.Success(PaymentIntentMappings.ToActionResult(
                    paymentIntentEntity,
                    requiresAction: false,
                    redirectUrl: null));
            },
            cancellationToken);
    }
}
