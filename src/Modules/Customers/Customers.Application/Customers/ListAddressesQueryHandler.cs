using BuildingBlocks.Application.Abstractions;

namespace Customers.Application.Customers;

public sealed class ListAddressesQueryHandler(ICustomerRepository customerRepository)
    : IQueryHandler<ListAddressesQuery, IReadOnlyCollection<AddressDto>>
{
    public async Task<IReadOnlyCollection<AddressDto>> Handle(
        ListAddressesQuery request,
        CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (customer is null)
        {
            return [];
        }

        return customer.Addresses
            .OrderByDescending(address => address.IsDefaultShipping)
            .ThenBy(address => address.Label, StringComparer.OrdinalIgnoreCase)
            .Select(CustomerMapper.ToDto)
            .ToArray();
    }
}
