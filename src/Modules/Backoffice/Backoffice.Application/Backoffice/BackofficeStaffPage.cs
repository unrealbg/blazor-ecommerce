namespace Backoffice.Application.Backoffice;

public sealed record BackofficeStaffPage(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyCollection<BackofficeStaffUserSummaryDto> Items);
