using Microsoft.EntityFrameworkCore;
using Orders.Application.Orders;
using Orders.Domain.Orders;

namespace Orders.Infrastructure.Persistence;

internal sealed class OrderRepository(OrdersDbContext dbContext) : IOrderRepository
{
    public Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        return dbContext.Orders.AddAsync(order, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyCollection<Order>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Orders
            .AsNoTracking()
            .OrderByDescending(order => order.CreatedOnUtc)
            .ToListAsync(cancellationToken);
    }
}
