using Microsoft.EntityFrameworkCore;
using Orders.Application.Orders;

namespace Orders.Infrastructure.Persistence;

internal sealed class CheckoutIdempotencyRepository(OrdersDbContext dbContext) : ICheckoutIdempotencyRepository
{
    public async Task<CheckoutIdempotencyRecord?> GetByKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return await dbContext.CheckoutIdempotencyRecords
            .AsNoTracking()
            .Where(record => record.IdempotencyKey == idempotencyKey)
            .Select(record => new CheckoutIdempotencyRecord(
                record.IdempotencyKey,
                record.CustomerId,
                record.OrderId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task AddAsync(
        string idempotencyKey,
        string customerId,
        Guid orderId,
        DateTime createdOnUtc,
        CancellationToken cancellationToken)
    {
        return dbContext.CheckoutIdempotencyRecords
            .AddAsync(
                new CheckoutIdempotency
                {
                    Id = Guid.NewGuid(),
                    IdempotencyKey = idempotencyKey,
                    CustomerId = customerId,
                    OrderId = orderId,
                    CreatedOnUtc = createdOnUtc,
                },
                cancellationToken)
            .AsTask();
    }
}
