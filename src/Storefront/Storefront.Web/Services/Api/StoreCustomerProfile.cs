namespace Storefront.Web.Services.Api;

public sealed record StoreCustomerProfile(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    bool IsEmailVerified,
    bool IsActive,
    IReadOnlyCollection<StoreCustomerAddress> Addresses);
