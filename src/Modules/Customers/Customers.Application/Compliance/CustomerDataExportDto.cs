using BuildingBlocks.Application.Contracts;

namespace Customers.Application.Compliance;

public sealed record CustomerDataExportDto(
    Guid CustomerId,
    Guid? UserId,
    string Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    bool IsEmailVerified,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyCollection<CustomerAddressExportDto> Addresses,
    IReadOnlyCollection<CustomerOrderExportRecord> Orders,
    IReadOnlyCollection<CustomerReviewExportRecord> Reviews,
    IReadOnlyCollection<CustomerQuestionExportRecord> Questions);