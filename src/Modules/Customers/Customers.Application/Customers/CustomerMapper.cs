using Customers.Domain.Customers;

namespace Customers.Application.Customers;

internal static class CustomerMapper
{
    public static CustomerDto ToDto(Customer customer)
    {
        return new CustomerDto(
            customer.Id,
            customer.Email,
            customer.FirstName,
            customer.LastName,
            customer.PhoneNumber,
            customer.IsEmailVerified,
            customer.IsActive,
            customer.Addresses
                .OrderBy(address => address.Label, StringComparer.OrdinalIgnoreCase)
                .Select(ToDto)
                .ToArray());
    }

    public static AddressDto ToDto(Address address)
    {
        return new AddressDto(
            address.Id,
            address.Label,
            address.FirstName,
            address.LastName,
            address.Company,
            address.Street1,
            address.Street2,
            address.City,
            address.PostalCode,
            address.CountryCode,
            address.Phone,
            address.IsDefaultShipping,
            address.IsDefaultBilling);
    }
}
