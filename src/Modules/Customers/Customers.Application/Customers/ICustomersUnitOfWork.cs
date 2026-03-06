namespace Customers.Application.Customers;

public interface ICustomersUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
