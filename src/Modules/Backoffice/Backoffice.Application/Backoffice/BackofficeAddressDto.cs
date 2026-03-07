namespace Backoffice.Application.Backoffice;

public sealed record BackofficeAddressDto(
    Guid? AddressId,
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
