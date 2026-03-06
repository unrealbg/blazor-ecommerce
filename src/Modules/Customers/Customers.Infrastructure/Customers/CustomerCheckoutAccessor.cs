using BuildingBlocks.Application.Contracts;
using Customers.Application.Customers;
using Customers.Domain.Customers;

namespace Customers.Infrastructure.Customers;

internal sealed class CustomerCheckoutAccessor(
    ICustomerRepository customerRepository,
    ICustomersUnitOfWork unitOfWork)
    : ICustomerCheckoutAccessor
{
    public async Task<CustomerCheckoutProfile> GetOrCreateGuestByEmailAsync(
        string email,
        string? firstName,
        string? lastName,
        string? phoneNumber,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var existingCustomer = await customerRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);
        if (existingCustomer is not null)
        {
            return ToCheckoutProfile(existingCustomer);
        }

        var createResult = Customer.CreateGuest(email, firstName, lastName, phoneNumber);
        if (createResult.IsFailure)
        {
            throw new InvalidOperationException(createResult.Error.Message);
        }

        await customerRepository.AddAsync(createResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToCheckoutProfile(createResult.Value);
    }

    public async Task<CustomerCheckoutProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByUserIdAsync(userId, cancellationToken);
        return customer is null ? null : ToCheckoutProfile(customer);
    }

    private static CustomerCheckoutProfile ToCheckoutProfile(Customer customer)
    {
        return new CustomerCheckoutProfile(
            customer.Id,
            customer.Email,
            customer.FirstName,
            customer.LastName,
            customer.PhoneNumber);
    }
}
