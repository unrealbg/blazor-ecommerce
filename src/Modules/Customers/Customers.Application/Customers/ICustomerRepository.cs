using Customers.Domain.Customers;

namespace Customers.Application.Customers;

public interface ICustomerRepository
{
    Task AddAsync(Customer customer, CancellationToken cancellationToken);

    Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken);

    Task<Customer?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<Customer?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
