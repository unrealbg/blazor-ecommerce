using Payments.Domain.Payments;

namespace Payments.Application.Payments;

public interface IPaymentIdempotencyRepository
{
    Task AddAsync(PaymentIdempotencyRecord record, CancellationToken cancellationToken);

    Task<PaymentIdempotencyRecord?> GetByOperationAndKeyAsync(
        string operation,
        string idempotencyKey,
        CancellationToken cancellationToken);
}
