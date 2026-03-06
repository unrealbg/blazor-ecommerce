using BuildingBlocks.Application.Abstractions;

namespace Payments.Application.Payments.GetPaymentIntentByOrder;

public sealed class GetPaymentIntentByOrderQueryHandler(
    IPaymentIntentRepository paymentIntentRepository,
    IPaymentTransactionRepository paymentTransactionRepository)
    : IQueryHandler<GetPaymentIntentByOrderQuery, PaymentIntentDetailsDto?>
{
    public async Task<PaymentIntentDetailsDto?> Handle(
        GetPaymentIntentByOrderQuery request,
        CancellationToken cancellationToken)
    {
        var paymentIntentEntity = await paymentIntentRepository.GetLatestByOrderIdAsync(request.OrderId, cancellationToken);
        if (paymentIntentEntity is null)
        {
            return null;
        }

        var transactions = await paymentTransactionRepository.ListByPaymentIntentIdAsync(
            paymentIntentEntity.Id,
            cancellationToken);

        return new PaymentIntentDetailsDto(
            paymentIntentEntity.Id,
            paymentIntentEntity.OrderId,
            paymentIntentEntity.CustomerId,
            paymentIntentEntity.Provider,
            paymentIntentEntity.Status.ToString(),
            paymentIntentEntity.Amount,
            paymentIntentEntity.Currency,
            paymentIntentEntity.ProviderPaymentIntentId,
            paymentIntentEntity.ClientSecret,
            paymentIntentEntity.FailureCode,
            paymentIntentEntity.FailureMessage,
            paymentIntentEntity.CreatedAtUtc,
            paymentIntentEntity.UpdatedAtUtc,
            paymentIntentEntity.CompletedAtUtc,
            transactions
                .OrderByDescending(transaction => transaction.CreatedAtUtc)
                .Select(PaymentIntentMappings.ToDto)
                .ToList());
    }
}
