using Microsoft.EntityFrameworkCore;
using Payments.Application.Payments;
using Payments.Domain.Payments;

namespace Payments.Infrastructure.Persistence;

internal sealed class PaymentTransactionRepository(PaymentsDbContext dbContext) : IPaymentTransactionRepository
{
    public Task AddAsync(PaymentTransaction paymentTransaction, CancellationToken cancellationToken)
    {
        return dbContext.PaymentTransactions.AddAsync(paymentTransaction, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyCollection<PaymentTransaction>> ListByPaymentIntentIdAsync(
        Guid paymentIntentId,
        CancellationToken cancellationToken)
    {
        return await dbContext.PaymentTransactions
            .AsNoTracking()
            .Where(transaction => transaction.PaymentIntentId == paymentIntentId)
            .OrderByDescending(transaction => transaction.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
