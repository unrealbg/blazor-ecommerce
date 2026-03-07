namespace Backoffice.Application.Backoffice;

public sealed record BackofficeStaffRoleCatalogItemDto(
    string RoleName,
    IReadOnlyCollection<string> Permissions);
