namespace Backoffice.Application.Backoffice;

public sealed record BackofficeStaffUserDetailDto(
    Guid Id,
    string Email,
    string? DisplayName,
    string? Department,
    bool IsStaff,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
