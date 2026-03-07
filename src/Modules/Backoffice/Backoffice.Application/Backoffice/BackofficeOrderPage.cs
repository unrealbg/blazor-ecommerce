namespace Backoffice.Application.Backoffice;

public sealed record BackofficeOrderPage(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyCollection<BackofficeOrderListItemDto> Items);
