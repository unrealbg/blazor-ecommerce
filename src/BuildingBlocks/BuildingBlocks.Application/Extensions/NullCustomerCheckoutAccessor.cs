using BuildingBlocks.Application.Contracts;

namespace BuildingBlocks.Application.Extensions;

internal sealed class NullCustomerCheckoutAccessor : ICustomerCheckoutAccessor
{
    public Task<CustomerCheckoutProfile> GetOrCreateGuestByEmailAsync(
        string email,
        string? firstName,
        string? lastName,
        string? phoneNumber,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new CustomerCheckoutProfile(
            Guid.Empty,
            email,
            firstName,
            lastName,
            phoneNumber));
    }

    public Task<CustomerCheckoutProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult<CustomerCheckoutProfile?>(null);
    }
}
