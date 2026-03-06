using Payments.Domain.Payments;

namespace Payments.Application.Payments;

public interface IPaymentTransactionRepository
{
    Task AddAsync(PaymentTransaction paymentTransaction, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PaymentTransaction>> ListByPaymentIntentIdAsync(
        Guid paymentIntentId,
        CancellationToken cancellationToken);
}
