namespace Backoffice.Application.Backoffice;

public sealed record BackofficeAuditPage(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyCollection<BackofficeAuditEntryDto> Items);
