using Orders.Domain.Orders;

namespace Orders.Application.Orders;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken);

    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken);
}
