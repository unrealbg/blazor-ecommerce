namespace Customers.Application.Customers;

public sealed record AddressDto(
    Guid Id,
    string Label,
    string FirstName,
    string LastName,
    string? Company,
    string Street1,
    string? Street2,
    string City,
    string PostalCode,
    string CountryCode,
    string? Phone,
    bool IsDefaultShipping,
    bool IsDefaultBilling);
