using Orders.Domain.Orders;

namespace Orders.Application.Orders;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Order>> ListAsync(CancellationToken cancellationToken);
}
