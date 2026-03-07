namespace BuildingBlocks.Application.Authorization;

public sealed record BackofficePermissionSnapshot(
    Guid UserId,
    string Email,
    string? DisplayName,
    string? Department,
    bool IsStaff,
    bool IsActive,
    DateTime? LastLoginAtUtc,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
