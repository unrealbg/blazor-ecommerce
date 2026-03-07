namespace Backoffice.Application.Backoffice;

public sealed record BackofficeCustomerSummaryDto(
    Guid Id,
    string Email,
    string? FullName,
    string? PhoneNumber,
    bool IsEmailVerified,
    bool IsActive,
    DateTime CreatedAtUtc,
    int OrderCount,
    DateTime? LastOrderAtUtc);
