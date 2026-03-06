using BuildingBlocks.Application.Abstractions;

namespace Payments.Application.Payments.GetPaymentIntent;

public sealed class GetPaymentIntentQueryHandler(
    IPaymentIntentRepository paymentIntentRepository,
    IPaymentTransactionRepository paymentTransactionRepository)
    : IQueryHandler<GetPaymentIntentQuery, PaymentIntentDetailsDto?>
{
    public async Task<PaymentIntentDetailsDto?> Handle(
        GetPaymentIntentQuery request,
        CancellationToken cancellationToken)
    {
        var paymentIntentEntity = await paymentIntentRepository.GetByIdAsync(request.PaymentIntentId, cancellationToken);
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
