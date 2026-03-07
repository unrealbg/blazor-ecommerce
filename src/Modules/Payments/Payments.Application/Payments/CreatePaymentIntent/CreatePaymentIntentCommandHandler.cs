using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Application.Diagnostics;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payments.Application.Providers;
using Payments.Domain.Payments;

namespace Payments.Application.Payments.CreatePaymentIntent;

public sealed class CreatePaymentIntentCommandHandler(
    IOrderPaymentService orderPaymentService,
    IPaymentIntentRepository paymentIntentRepository,
    IPaymentTransactionRepository paymentTransactionRepository,
    IPaymentIdempotencyRepository paymentIdempotencyRepository,
    IPaymentProviderFactory paymentProviderFactory,
    IPaymentsUnitOfWork unitOfWork,
    IClock clock,
    IOptions<PaymentsModuleOptions> options,
    ILogger<CreatePaymentIntentCommandHandler> logger)
    : ICommandHandler<CreatePaymentIntentCommand, PaymentIntentActionResult>
{
    private const string CreateOperation = "create-intent";
    private readonly PaymentsModuleOptions options = options.Value;

    public async Task<Result<PaymentIntentActionResult>> Handle(
        CreatePaymentIntentCommand request,
        CancellationToken cancellationToken)
    {
        using var activity = CommerceDiagnostics.StartActivity("payments.create_intent");
        activity?.SetTag("order.id", request.OrderId);

        var idempotencyKey = request.IdempotencyKey.Trim();
        var existingRecord = await paymentIdempotencyRepository.GetByOperationAndKeyAsync(
            CreateOperation,
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

        var orderSnapshot = await orderPaymentService.GetByIdAsync(request.OrderId, cancellationToken);
        if (orderSnapshot is null)
        {
            return Result<PaymentIntentActionResult>.Failure(new Error(
                "payments.order.not_payable",
                "Order is not payable."));
        }

        if (!IsPayableStatus(orderSnapshot.Status))
        {
            return Result<PaymentIntentActionResult>.Failure(new Error(
                "payments.order.not_payable",
                "Order is not in a payable state."));
        }

        if (orderSnapshot.TotalAmount <= 0m)
        {
            return Result<PaymentIntentActionResult>.Failure(new Error(
                "payments.amount.mismatch",
                "Order total amount is invalid for payment."));
        }

        var providerName = string.IsNullOrWhiteSpace(request.Provider)
            ? options.DefaultProvider
            : request.Provider.Trim();

        return await unitOfWork.ExecuteInTransactionAsync(
            async innerCancellationToken =>
            {
                var idempotencyInTransaction = await paymentIdempotencyRepository.GetByOperationAndKeyAsync(
                    CreateOperation,
                    idempotencyKey,
                    innerCancellationToken);
                if (idempotencyInTransaction is not null)
                {
                    var paymentIntent = await paymentIntentRepository.GetByIdAsync(
                        idempotencyInTransaction.PaymentIntentId,
                        innerCancellationToken);
                    if (paymentIntent is not null)
                    {
                        return Result<PaymentIntentActionResult>.Success(PaymentIntentMappings.ToActionResult(
                            paymentIntent,
                            paymentIntent.Status == PaymentIntentStatus.RequiresAction,
                            null));
                    }
                }

                var latestIntent = await paymentIntentRepository.GetLatestByOrderIdAsync(
                    request.OrderId,
                    innerCancellationToken);

                if (latestIntent is not null && latestIntent.IsActive)
                {
                    var reuseRecordResult = PaymentIdempotencyRecord.Create(
                        CreateOperation,
                        idempotencyKey,
                        latestIntent.Id,
                        clock.UtcNow);
                    if (reuseRecordResult.IsFailure)
                    {
                        return Result<PaymentIntentActionResult>.Failure(reuseRecordResult.Error);
                    }

                    await paymentIdempotencyRepository.AddAsync(reuseRecordResult.Value, innerCancellationToken);
                    await unitOfWork.SaveChangesAsync(innerCancellationToken);

                    return Result<PaymentIntentActionResult>.Success(PaymentIntentMappings.ToActionResult(
                        latestIntent,
                        latestIntent.Status == PaymentIntentStatus.RequiresAction,
                        null));
                }

                var createIntentResult = PaymentIntent.Create(
                    request.OrderId,
                    TryParseCustomerId(orderSnapshot.CustomerId),
                    providerName,
                    orderSnapshot.TotalAmount,
                    orderSnapshot.Currency,
                    idempotencyKey,
                    clock.UtcNow);
                if (createIntentResult.IsFailure)
                {
                    return Result<PaymentIntentActionResult>.Failure(createIntentResult.Error);
                }

                var paymentIntentEntity = createIntentResult.Value;
                await paymentIntentRepository.AddAsync(paymentIntentEntity, innerCancellationToken);

                IPaymentProvider provider;
                try
                {
                    provider = paymentProviderFactory.Resolve(providerName);
                }
                catch (InvalidOperationException)
                {
                    CommerceDiagnostics.RecordPaymentIntentCreation(false, providerName);
                    return Result<PaymentIntentActionResult>.Failure(new Error(
                        "payments.provider.unavailable",
                        "Requested payment provider is unavailable."));
                }

                PaymentProviderCreateResponse providerResponse;
                try
                {
                    providerResponse = await provider.CreatePaymentIntentAsync(
                        new PaymentProviderCreateRequest(
                            request.OrderId,
                            paymentIntentEntity.Id,
                            paymentIntentEntity.Amount,
                            paymentIntentEntity.Currency,
                            request.CustomerEmail,
                            ReturnUrl: null,
                            CallbackUrl: null,
                            new Dictionary<string, string>
                            {
                                ["orderId"] = request.OrderId.ToString("D"),
                                ["paymentIntentId"] = paymentIntentEntity.Id.ToString("D"),
                            }),
                        innerCancellationToken);
                }
                catch (Exception)
                {
                    logger.LogWarning(
                        "Payment provider unavailable while creating payment intent for order {OrderId} provider {Provider}",
                        request.OrderId,
                        providerName);
                    CommerceDiagnostics.RecordPaymentIntentCreation(false, providerName);
                    return Result<PaymentIntentActionResult>.Failure(new Error(
                        "payments.provider.unavailable",
                        "Payment provider is unavailable."));
                }

                var applyResult = paymentIntentEntity.ApplyProviderCreation(
                    providerResponse.ProviderPaymentIntentId,
                    providerResponse.ClientSecret,
                    providerResponse.Status,
                    providerResponse.FailureCode,
                    providerResponse.FailureMessage,
                    clock.UtcNow);
                if (applyResult.IsFailure)
                {
                    return Result<PaymentIntentActionResult>.Failure(applyResult.Error);
                }

                var transactionType = providerResponse.Status == PaymentIntentStatus.Captured
                    ? PaymentTransactionType.Charge
                    : PaymentTransactionType.Authorization;

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
                    CreateOperation,
                    idempotencyKey,
                    paymentIntentEntity.Id,
                    clock.UtcNow);
                if (createIdempotencyResult.IsFailure)
                {
                    return Result<PaymentIntentActionResult>.Failure(createIdempotencyResult.Error);
                }

                await paymentIdempotencyRepository.AddAsync(createIdempotencyResult.Value, innerCancellationToken);
                await unitOfWork.SaveChangesAsync(innerCancellationToken);

                logger.LogInformation(
                    "Created payment intent {PaymentIntentId} for order {OrderId} provider {Provider}",
                    paymentIntentEntity.Id,
                    request.OrderId,
                    providerName);
                CommerceDiagnostics.RecordPaymentIntentCreation(true, providerName);

                return Result<PaymentIntentActionResult>.Success(PaymentIntentMappings.ToActionResult(
                    paymentIntentEntity,
                    providerResponse.RequiresAction,
                    providerResponse.RedirectUrl));
            },
            cancellationToken);
    }

    private static bool IsPayableStatus(string status)
    {
        return status.Equals("PendingPayment", StringComparison.Ordinal) ||
               status.Equals("PaymentFailed", StringComparison.Ordinal);
    }

    private static Guid? TryParseCustomerId(string customerId)
    {
        return Guid.TryParse(customerId, out var parsedCustomerId) ? parsedCustomerId : null;
    }
}
