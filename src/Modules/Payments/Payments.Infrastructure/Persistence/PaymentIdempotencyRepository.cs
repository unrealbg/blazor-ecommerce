using Microsoft.EntityFrameworkCore;
using Payments.Application.Payments;
using Payments.Domain.Payments;

namespace Payments.Infrastructure.Persistence;

internal sealed class PaymentIdempotencyRepository(PaymentsDbContext dbContext) : IPaymentIdempotencyRepository
{
    public Task AddAsync(PaymentIdempotencyRecord record, CancellationToken cancellationToken)
    {
        return dbContext.PaymentIdempotencyRecords.AddAsync(record, cancellationToken).AsTask();
    }

    public Task<PaymentIdempotencyRecord?> GetByOperationAndKeyAsync(
        string operation,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return dbContext.PaymentIdempotencyRecords.SingleOrDefaultAsync(
            record => record.Operation == operation && record.IdempotencyKey == idempotencyKey,
            cancellationToken);
    }
}
