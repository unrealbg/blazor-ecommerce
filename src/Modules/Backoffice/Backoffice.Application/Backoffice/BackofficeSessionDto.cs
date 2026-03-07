namespace Backoffice.Application.Backoffice;

public sealed record BackofficeSessionDto(
    Guid UserId,
    string Email,
    string? DisplayName,
    string? Department,
    bool IsActive,
    DateTime? LastLoginAtUtc,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
