using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Payments.Application.Providers;
using Payments.Domain.Payments;

namespace Payments.Application.Payments.RefundPaymentIntent;

public sealed class RefundPaymentIntentCommandHandler(
    IPaymentIntentRepository paymentIntentRepository,
    IPaymentTransactionRepository paymentTransactionRepository,
    IPaymentProviderFactory paymentProviderFactory,
    IPaymentsUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<RefundPaymentIntentCommand, PaymentIntentActionResult>
{
    public async Task<Result<PaymentIntentActionResult>> Handle(
        RefundPaymentIntentCommand request,
        CancellationToken cancellationToken)
    {
        var paymentIntentEntity = await paymentIntentRepository.GetByIdAsync(request.PaymentIntentId, cancellationToken);
        if (paymentIntentEntity is null)
        {
            return Result<PaymentIntentActionResult>.Failure(new Error(
                "payments.intent.not_found",
                "Payment intent was not found."));
        }

        if (paymentIntentEntity.Status is not (PaymentIntentStatus.Captured or PaymentIntentStatus.PartiallyRefunded))
        {
            return Result<PaymentIntentActionResult>.Failure(new Error(
                "payments.refund.not_allowed",
                "Refund is allowed only for captured payments."));
        }

        var refundAmount = request.Amount ?? paymentIntentEntity.Amount;
        if (refundAmount <= 0m || refundAmount > paymentIntentEntity.Amount)
        {
            return Result<PaymentIntentActionResult>.Failure(new Error(
                "payments.refund.amount.invalid",
                "Refund amount must be greater than zero and cannot exceed captured amount."));
        }

        return await unitOfWork.ExecuteInTransactionAsync(
            async innerCancellationToken =>
            {
                PaymentProviderRefundResponse providerResponse;

                if (string.IsNullOrWhiteSpace(paymentIntentEntity.ProviderPaymentIntentId))
                {
                    var isPartialFallback = refundAmount < paymentIntentEntity.Amount;
                    providerResponse = new PaymentProviderRefundResponse(
                        isPartialFallback ? PaymentIntentStatus.PartiallyRefunded : PaymentIntentStatus.Refunded,
                        refundAmount,
                        isPartialFallback,
                        ProviderTransactionId: null,
                        RawReference: null,
                        MetadataJson: null,
                        FailureCode: null,
                        FailureMessage: null);
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
                        providerResponse = await provider.RefundPaymentAsync(
                            new PaymentProviderRefundRequest(
                                paymentIntentEntity.Id,
                                paymentIntentEntity.ProviderPaymentIntentId,
                                paymentIntentEntity.Amount,
                                request.Amount,
                                paymentIntentEntity.Currency,
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

                if (providerResponse.Status is not (PaymentIntentStatus.Refunded or PaymentIntentStatus.PartiallyRefunded))
                {
                    return Result<PaymentIntentActionResult>.Failure(new Error(
                        "payments.refund.failed",
                        providerResponse.FailureMessage ?? "Refund failed."));
                }

                var applyResult = paymentIntentEntity.ApplyProviderRefund(providerResponse.IsPartial, clock.UtcNow);
                if (applyResult.IsFailure)
                {
                    return Result<PaymentIntentActionResult>.Failure(applyResult.Error);
                }

                var createTransactionResult = PaymentTransaction.Create(
                    paymentIntentEntity.Id,
                    PaymentTransactionType.Refund,
                    providerResponse.ProviderTransactionId,
                    providerResponse.RefundedAmount,
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
                await unitOfWork.SaveChangesAsync(innerCancellationToken);

                return Result<PaymentIntentActionResult>.Success(PaymentIntentMappings.ToActionResult(
                    paymentIntentEntity,
                    requiresAction: false,
                    redirectUrl: null));
            },
            cancellationToken);
    }
}
