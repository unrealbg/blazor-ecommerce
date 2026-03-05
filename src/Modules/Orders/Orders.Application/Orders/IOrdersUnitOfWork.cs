namespace Orders.Application.Orders;

public interface IOrdersUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
