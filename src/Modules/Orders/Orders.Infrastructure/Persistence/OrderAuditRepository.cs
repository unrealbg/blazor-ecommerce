using Orders.Application.Orders;

namespace Orders.Infrastructure.Persistence;

internal sealed class OrderAuditRepository(OrdersDbContext dbContext) : IOrderAuditRepository
{
    public Task AddAsync(
        Guid eventId,
        Guid orderId,
        string customerId,
        string currency,
        decimal totalAmount,
        DateTime loggedOnUtc,
        CancellationToken cancellationToken)
    {
        return dbContext.OrderAudits
            .AddAsync(
                new OrderAudit
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    OrderId = orderId,
                    CustomerId = customerId,
                    Currency = currency,
                    TotalAmount = totalAmount,
                    LoggedOnUtc = loggedOnUtc,
                },
                cancellationToken)
            .AsTask();
    }
}
