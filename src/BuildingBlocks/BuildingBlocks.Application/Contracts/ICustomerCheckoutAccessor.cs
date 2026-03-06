namespace BuildingBlocks.Application.Contracts;

public interface ICustomerCheckoutAccessor
{
    Task<CustomerCheckoutProfile> GetOrCreateGuestByEmailAsync(
        string email,
        string? firstName,
        string? lastName,
        string? phoneNumber,
        CancellationToken cancellationToken);

    Task<CustomerCheckoutProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
