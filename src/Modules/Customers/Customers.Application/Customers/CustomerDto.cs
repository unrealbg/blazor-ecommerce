namespace Customers.Application.Customers;

public sealed record CustomerDto(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    bool IsEmailVerified,
    bool IsActive,
    IReadOnlyCollection<AddressDto> Addresses);
