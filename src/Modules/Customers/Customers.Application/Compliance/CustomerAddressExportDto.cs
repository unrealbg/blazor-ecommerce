namespace Customers.Application.Compliance;

public sealed record CustomerAddressExportDto(
    Guid AddressId,
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
    bool IsDefaultBilling,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);