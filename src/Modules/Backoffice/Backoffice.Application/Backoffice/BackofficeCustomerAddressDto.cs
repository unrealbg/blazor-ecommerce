namespace Backoffice.Application.Backoffice;

public sealed record BackofficeCustomerAddressDto(
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
    bool IsDefaultBilling,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
