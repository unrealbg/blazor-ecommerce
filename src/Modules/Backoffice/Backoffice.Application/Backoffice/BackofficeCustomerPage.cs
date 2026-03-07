namespace Backoffice.Application.Backoffice;

public sealed record BackofficeCustomerPage(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyCollection<BackofficeCustomerSummaryDto> Items);
