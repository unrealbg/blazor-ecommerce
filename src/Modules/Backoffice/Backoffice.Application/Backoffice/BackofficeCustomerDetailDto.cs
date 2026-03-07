namespace Backoffice.Application.Backoffice;

public sealed record BackofficeCustomerDetailDto(
    Guid Id,
    Guid? UserId,
    string Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    bool IsEmailVerified,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyCollection<BackofficeCustomerAddressDto> Addresses,
    IReadOnlyCollection<BackofficeOrderListItemDto> Orders,
    IReadOnlyCollection<BackofficeCustomerActivityItemDto> Activity);
