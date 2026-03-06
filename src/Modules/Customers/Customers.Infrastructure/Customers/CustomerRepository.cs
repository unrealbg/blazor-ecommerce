using Customers.Application.Customers;
using Customers.Domain.Customers;
using Customers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Customers.Infrastructure.Customers;

internal sealed class CustomerRepository(CustomersDbContext dbContext) : ICustomerRepository
{
    public Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        return dbContext.Customers.AddAsync(customer, cancellationToken).AsTask();
    }

    public Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken)
    {
        return dbContext.Customers
            .Include(customer => customer.Addresses)
            .FirstOrDefaultAsync(customer => customer.Id == customerId, cancellationToken);
    }

    public Task<Customer?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return dbContext.Customers
            .Include(customer => customer.Addresses)
            .FirstOrDefaultAsync(customer => customer.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public Task<Customer?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.Customers
            .Include(customer => customer.Addresses)
            .FirstOrDefaultAsync(customer => customer.UserId == userId, cancellationToken);
    }
}
